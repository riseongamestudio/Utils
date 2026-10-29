using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Picks the type a polymorphic field is set to, generic types included: first among what the field can hold, then, for a generic type<br/>
    /// whose arguments the field does not fix, each argument in turn, closing any generic argument the same way first.<br/>
    /// Every step lists types as Odin's type selector does, under the same categories and names, hidden types left out,<br/>
    /// in one <see cref="ChoiceSearchWindow"/> drop-down that moves to the next step without closing.
    /// </summary>
    public static class GenericTypePicker {
        private const string SLOT = "▢";

        private static readonly string[] SKIPPED_ASSEMBLIES = {
            "System", "Microsoft", "Mono", "mscorlib", "netstandard", "nunit", "Unity.", "UnityEditor", "Sirenix", "JetBrains", "Bee", "ExCSS"
        };

        private static readonly Type[] BUILT_IN = {
            typeof(bool), typeof(int), typeof(long), typeof(float), typeof(double), typeof(string), typeof(object),
            typeof(List<>), typeof(HashSet<>), typeof(Dictionary<,>)
        };

        // Listed on a background thread after every domain reload, so the first argument step rarely waits for it.
        private static readonly Lazy<Type[]> ARGUMENT_TYPES = new(ListArgumentTypes);

        [InitializeOnLoadMethod]
        private static void WarmUp() {
            System.Threading.Tasks.Task.Run(() => ARGUMENT_TYPES.Value);
        }

        /// <param name="field">The field, in the current GUI space; call it while the Inspector draws.</param>
        /// <param name="baseType">The type the field is declared as.</param>
        /// <param name="rule">Narrows the arguments of the generic type picked into the field; null offers what Odin offers and no generic type to build.</param>
        /// <param name="done">Gets the type picked, or null for None; not called when the pick is dropped.</param>
        /// <param name="title">The window's title; null titles it with <paramref name="baseType"/>, empty leaves it without, as under a field already named.</param>
        public static void Pick(Rect field, Type baseType, AllowGenericsAttribute rule, Action<Type> done, string title = null) {
            var inferred = new Dictionary<Type, Type[]>();
            var choices = ListChoices(() => ListFieldTypes(baseType, rule, inferred), true,
                type => type.IsGenericTypeDefinition ? Name(type, inferred[type]) : null,
                type => type.IsGenericTypeDefinition ? null : rule?.GetDisabledReason(type));

            ChoiceSearchWindow window = null;
            window = ChoiceSearchWindow.Open(field, title ?? baseType.GetNiceName(), choices, (Type picked) => {
                if (picked is { IsGenericTypeDefinition: true }) Close(window, picked, inferred[picked], new List<Frame>(), rule, done);
                else done(picked);
            });
        }

        // The types under the category their [TypeRegistryItem] gives, as Odin's type selector files them, or else under their namespace,
        // by their registry name or nice name, a duplicate told apart by its namespace, and the None row first when asked for.
        // The types are only listed once the window enumerates the choices, on screen.
        private static IEnumerable<SearchChoice<Type>> ListChoices(Func<IEnumerable<Type>> types, bool hasNone, Func<Type, string> name, Func<Type, string> disabledReason) {
            if (hasNone) yield return new SearchChoice<Type>("None", null);

            var listed = types().Select(type => (Path: GetPath(type), Type: type)).ToList();
            var duplicates = listed.GroupBy(choice => choice.Path).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();

            foreach (var choice in listed) {
                var path = duplicates.Contains(choice.Path) ? $"{choice.Path} ({choice.Type.Namespace})" : choice.Path;
                yield return new SearchChoice<Type>(path, choice.Type, disabledReason?.Invoke(choice.Type), () => GetIcon(choice.Type));
            }

            string GetPath(Type type) {
                var registry = type.GetCustomAttribute<TypeRegistryItemAttribute>();
                var label = name?.Invoke(type) ?? (string.IsNullOrEmpty(registry?.Name) ? type.GetNiceName() : registry.Name);
                var category = registry != null ? registry.CategoryPath?.Trim('/') : type.Namespace;
                return string.IsNullOrEmpty(category) ? label : $"{category}/{label}";
            }
        }

        // Unity's own icon for its object types; other types go without, as the window leaves room for one either way.
        private static Texture2D GetIcon(Type type) {
            return typeof(Object).IsAssignableFrom(type) ? AssetPreview.GetMiniTypeThumbnail(type) : null;
        }

        // What Odin offers for the field. When generics are allowed, Odin's generic types are worked out again here instead,
        // since Odin fills type arguments by position, and the generic definitions it leaves out are added:
        // closed when the field fixes every argument, to be finished by picking otherwise, with what the field fixes kept in `inferred`.
        private static IEnumerable<Type> ListFieldTypes(Type baseType, AllowGenericsAttribute rule, Dictionary<Type, Type[]> inferred) {
            var offered = TypeRegistry.GetInstantiableInheritors(baseType, false, true).ToList();
            if (rule == null) return offered;

            offered.RemoveAll(type => type.IsGenericType);

            foreach (var type in TypeCache.GetTypesDerivedFrom(baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType)) {
                if (!type.IsGenericTypeDefinition || type.IsAbstract || type.IsInterface) continue;

                var arguments = Infer(type, baseType);
                if (arguments == null) continue;

                if (arguments.Contains(null)) {
                    inferred[type] = arguments;
                    offered.Add(type);
                } else if (type.AreGenericConstraintsSatisfiedBy(arguments) && type.MakeGenericType(arguments) is var closed && baseType.IsAssignableFrom(closed)) {
                    offered.Add(closed);
                }
            }

            return offered;
        }

        // The arguments of `definition` that the field fixes, null where it does not; null as a whole when no closing of it fits the field.
        // An argument the field's type takes through variance, such as object for an `in` parameter, is checked by assignability.
        private static Type[] Infer(Type definition, Type baseType) {
            var count = definition.GetGenericArguments().Length;
            if (!baseType.IsGenericType) return new Type[count];

            var target = baseType.GetGenericTypeDefinition();
            foreach (var candidate in ListBases(definition)) {
                if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != target) continue;

                var arguments = new Type[count];
                if (UnifyVariant(candidate, baseType, arguments)) return arguments;
            }

            return null;

            static IEnumerable<Type> ListBases(Type definition) {
                for (var type = definition; type != null; type = type.BaseType) yield return type;
                foreach (var contract in definition.GetInterfaces()) yield return contract;
            }

            static bool UnifyVariant(Type pattern, Type actual, Type[] arguments) {
                var parameters = pattern.GetGenericTypeDefinition().GetGenericArguments();
                var patterns = pattern.GetGenericArguments();
                var actuals = actual.GetGenericArguments();

                for (var i = 0; i < patterns.Length; i++) {
                    if (patterns[i].ContainsGenericParameters) {
                        if (!Unify(patterns[i], actuals[i], arguments)) return false;
                    } else if (!FitsVariance(patterns[i], actuals[i], parameters[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask)) {
                        return false;
                    }
                }

                return true;
            }

            static bool Unify(Type pattern, Type actual, Type[] arguments) {
                if (pattern.IsGenericParameter) {
                    ref var bound = ref arguments[pattern.GenericParameterPosition];
                    bound ??= actual;
                    return bound == actual;
                }

                if (!pattern.ContainsGenericParameters) return pattern == actual;
                if (pattern.IsArray) return actual.IsArray && pattern.GetArrayRank() == actual.GetArrayRank() && Unify(pattern.GetElementType(), actual.GetElementType(), arguments);
                if (!pattern.IsGenericType || !actual.IsGenericType || pattern.GetGenericTypeDefinition() != actual.GetGenericTypeDefinition()) return false;

                var patterns = pattern.GetGenericArguments();
                var actuals = actual.GetGenericArguments();
                for (var i = 0; i < patterns.Length; i++) {
                    if (!Unify(patterns[i], actuals[i], arguments)) return false;
                }

                return true;
            }

            static bool FitsVariance(Type pattern, Type actual, GenericParameterAttributes variance) {
                if (pattern == actual) return true;
                if (pattern.IsValueType || actual.IsValueType) return false;

                return variance switch {
                    GenericParameterAttributes.Covariant => actual.IsAssignableFrom(pattern),
                    GenericParameterAttributes.Contravariant => pattern.IsAssignableFrom(actual),
                    _ => false
                };
            }
        }

        // A generic definition as the field leaves it, by its type registry name when it has one:
        // the arguments the field fixes named, the rest left as their parameter names.
        private static string Name(Type definition, Type[] arguments) {
            var name = definition.GetCustomAttribute<TypeRegistryItemAttribute>()?.Name ?? definition.Name[..definition.Name.IndexOf('`')];
            var parameters = definition.GetGenericArguments();
            return $"{name}<{string.Join(", ", parameters.Select((parameter, i) => arguments[i]?.GetNiceName() ?? parameter.Name))}>";
        }

        // Fills the type arguments of a generic definition one by one, closing any generic argument the same way first.
        // The rule narrows the arguments of the definition picked into the field only, the first one in the chain.
        // The arguments already known, those the field fixes, are skipped.
        private static void Close(ChoiceSearchWindow window, Type definition, Type[] known, List<Frame> chain, AllowGenericsAttribute rule, Action<Type> done) {
            var isPickedIntoField = chain.Count is 0;
            var frame = new Frame(definition, known);
            chain.Add(frame);
            Next();

            void Next() {
                while (frame.index < frame.arguments.Length && frame.arguments[frame.index] != null) frame.index++;

                if (frame.index == frame.arguments.Length) {
                    chain.Remove(frame);

                    // Constraints that tie one argument to another are only checked here, once all arguments are known.
                    if (!definition.AreGenericConstraintsSatisfiedBy(frame.arguments)) {
                        Debug.LogError($"{string.Join(", ", frame.arguments.Select(argument => argument.GetNiceName()))} do not satisfy the constraints of {definition.GetNiceName()}.");
                        return;
                    }

                    var closed = definition.MakeGenericType(frame.arguments);
                    var reason = isPickedIntoField ? rule.GetDisabledReason(closed) : null;
                    if (reason == null) done(closed);
                    else Debug.LogError($"{closed.GetNiceName()} cannot be picked: {reason}.");
                    return;
                }

                var parameter = frame.parameters[frame.index];

                // Listed while the window shows its build progress: the first time lists every type of the game and Unity.
                var choices = ListChoices(() => GetArgumentTypes().Where(type => Fits(parameter, type) && (!isPickedIntoField || rule.Accepts(parameter, type))), false, null, null);
                window.Present($"{Describe(chain, 0)}   ·   {parameter.Name}", choices, (Type picked) => {
                    if (picked.IsGenericTypeDefinition) Close(window, picked, new Type[picked.GetGenericArguments().Length], chain, rule, Take);
                    else Take(picked);
                });
            }

            void Take(Type argument) {
                frame.arguments[frame.index++] = argument;
                Next();
            }

            // The constraints a single argument can be checked against on its own; a generic definition is let through, to be closed and checked later.
            static bool Fits(Type parameter, Type type) {
                if (type.IsGenericTypeDefinition) return true;

                var flags = parameter.GenericParameterAttributes;
                if ((flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && type.IsValueType) return false;
                if ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)) return false;
                if ((flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !type.IsValueType && (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)) return false;

                foreach (var constraint in parameter.GetGenericParameterConstraints()) {
                    if (!constraint.ContainsGenericParameters && !constraint.IsAssignableFrom(type)) return false;
                }

                return true;
            }

            // The type being built, with the filled arguments named, the slot being filled marked and the rest left as their parameter names.
            static string Describe(List<Frame> chain, int depth) {
                var frame = chain[depth];
                var name = frame.definition.Name;
                var text = new StringBuilder(name[..name.IndexOf('`')]).Append('<');

                for (var i = 0; i < frame.parameters.Length; i++) {
                    if (i > 0) text.Append(", ");

                    if (i == frame.index) text.Append(depth + 1 < chain.Count ? Describe(chain, depth + 1) : SLOT);
                    else if (frame.arguments[i] != null) text.Append(frame.arguments[i].GetNiceName());
                    else text.Append(frame.parameters[i].Name);
                }

                return text.Append('>').ToString();
            }
        }

        // Every public type a type argument could be, built once: the built-in ones, then the types of the game and Unity runtime assemblies.
        private static Type[] GetArgumentTypes() {
            return ARGUMENT_TYPES.Value;
        }

        private static Type[] ListArgumentTypes() {
            return BUILT_IN.Concat(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic && !SKIPPED_ASSEMBLIES.Any(skipped => assembly.GetName().Name.StartsWith(skipped)))
                    .SelectMany(ListExportedTypes)
                    .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), false) && type.Namespace?.StartsWith(nameof(UnityEditor)) is not true))
                .ToArray();

            static IEnumerable<Type> ListExportedTypes(Assembly assembly) {
                try {
                    return assembly.GetExportedTypes();
                } catch (Exception) {
                    return Type.EmptyTypes;
                }
            }
        }

        private sealed class Frame {
            public readonly Type definition;
            public readonly Type[] parameters;
            public readonly Type[] arguments;
            public int index;

            public Frame(Type definition, Type[] known) {
                this.definition = definition;
                parameters = definition.GetGenericArguments();
                arguments = (Type[])known.Clone();
            }
        }
    }
}
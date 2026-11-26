using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RiseOn.Utils.Generator {
    /// <summary>
    /// Compiles into the game's assembly, for the one class implementing IEditorPauseHandler, the registration that makes it the Editor's pause handler.<br/>
    /// A second such class is an error, and so are an interface extending IEditorPauseHandler and a class the pack cannot make: abstract, generic,
    /// nested, or without a constructor taking no parameters.<br/>
    /// Nothing is written to disk, so nobody can edit it.
    /// </summary>
    [Generator]
    public sealed class RiseOnUtilsGenerator : ISourceGenerator {
        private const string PackAssembly = "RiseOn.Utils";
        private const string HandlerType  = "RiseOn.Utils.IEditorPauseHandler";

        private static readonly DiagnosticDescriptor cannotMake = new(
            "RISEONUTILS001", "Editor pause handler the pack cannot make", "{0} implements IEditorPauseHandler, so it must be a class the Utils pack can make: neither abstract, generic nor nested, with a constructor taking no parameters that is not private",
            "RiseOn.Utils", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor notAlone = new(
            "RISEONUTILS002", "Several Editor pause handlers", "{0} implements IEditorPauseHandler, and so does {1}; the Editor pauses the game one way, so only one class may",
            "RiseOn.Utils", DiagnosticSeverity.Error, true);

        private static readonly DiagnosticDescriptor subInterface = new(
            "RISEONUTILS003", "Interface extends IEditorPauseHandler", "{0} extends IEditorPauseHandler; the handler class implements IEditorPauseHandler itself, so no interface may extend it",
            "RiseOn.Utils", DiagnosticSeverity.Error, true);

        public void Initialize(GeneratorInitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }

        public void Execute(GeneratorExecutionContext context) {
            if (context.Compilation.AssemblyName is PackAssembly) return;

            var handler = context.Compilation.GetTypeByMetadataName(HandlerType);
            if (handler == null || context.SyntaxReceiver is not Receiver receiver) return;

            foreach (var declaration in receiver.Interfaces) {
                var model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                if (model.GetDeclaredSymbol(declaration) is INamedTypeSymbol type && Implements(type, handler)) {
                    context.ReportDiagnostic(Diagnostic.Create(subInterface, declaration.Identifier.GetLocation(), type.ToDisplayString()));
                }
            }

            // Every class of this assembly implementing it, with the declaration that names it first.
            var found = new Dictionary<INamedTypeSymbol, ClassDeclarationSyntax>(SymbolEqualityComparer.Default);
            var mine  = new List<INamedTypeSymbol>();
            foreach (var declaration in receiver.Classes) {
                var model = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type || !Implements(type, handler) || found.ContainsKey(type)) continue;

                found.Add(type, declaration);
                mine.Add(type);
            }

            // Assemblies this one references can hold one too; sibling assemblies the Editor checks.
            var all = mine.Concat(Referenced(context.Compilation, handler)).ToList();
            foreach (var type in mine) {
                var declaration = found[type];
                if (!CanMake(type)) {
                    context.ReportDiagnostic(Diagnostic.Create(cannotMake, declaration.Identifier.GetLocation(), type.ToDisplayString()));
                    continue;
                }

                var other = all.FirstOrDefault(each => !SymbolEqualityComparer.Default.Equals(each, type));
                if (other != null) {
                    context.ReportDiagnostic(Diagnostic.Create(notAlone, declaration.Identifier.GetLocation(), type.ToDisplayString(), other.ToDisplayString()));
                    continue;
                }

                context.AddSource($"{type.ToDisplayString().Replace('.', '_')}.IEditorPauseHandler.g.cs", SourceText.From(Registration(type), Encoding.UTF8));
            }
        }

        private static bool CanMake(INamedTypeSymbol type) {
            return !type.IsAbstract && !type.IsGenericType && type.ContainingType == null
                && type.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility is not Accessibility.Private);
        }

        // Only assemblies that reference the pack can hold a handler, which keeps the search off the engine's own.
        private static IEnumerable<INamedTypeSymbol> Referenced(Compilation compilation, INamedTypeSymbol handler) {
            foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols) {
                if (assembly.Name is PackAssembly || !assembly.Modules.Any(module => module.ReferencedAssemblySymbols.Any(each => each.Name is PackAssembly))) continue;

                foreach (var type in Types(assembly.GlobalNamespace)) {
                    if (type.TypeKind is TypeKind.Class && CanMake(type) && Implements(type, handler)) yield return type;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol space) {
            foreach (var type in space.GetTypeMembers()) yield return type;
            foreach (var child in space.GetNamespaceMembers()) {
                foreach (var type in Types(child)) yield return type;
            }
        }

        // Editor only, as the Editor alone pauses through it; a class of its own, so the game's class needs no partial.
        private static string Registration(INamedTypeSymbol type) {
            var name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var code = new StringBuilder();
            code.Append("// <auto-generated/>\n");
            code.Append("#if UNITY_EDITOR\n");
            code.Append($"internal static class RiseOnEditorPauseHandler_{type.ToDisplayString().Replace('.', '_')} {{\n");
            code.Append("    [global::UnityEngine.RuntimeInitializeOnLoadMethod(global::UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]\n");
            code.Append($"    private static void Register() => global::RiseOn.Utils.IEditorPauseHandler.Register(() => new {name}());\n");
            code.Append("}\n");
            code.Append("#endif\n");
            return code.ToString();
        }

        private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol root) {
            return type.AllInterfaces.Contains(root, SymbolEqualityComparer.Default);
        }

        private sealed class Receiver : ISyntaxReceiver {
            public readonly List<ClassDeclarationSyntax> Classes = new();
            public readonly List<InterfaceDeclarationSyntax> Interfaces = new();

            public void OnVisitSyntaxNode(SyntaxNode node) {
                switch (node) {
                    case ClassDeclarationSyntax { BaseList: not null } declaration:
                        Classes.Add(declaration);
                        break;
                    case InterfaceDeclarationSyntax { BaseList: not null } declaration:
                        Interfaces.Add(declaration);
                        break;
                }
            }
        }
    }
}
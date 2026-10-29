using System;

namespace RiseOn.Utils {
    /// <summary>
    /// Lets the type picker of a <c>[SerializeReference]</c> field also offer generic types whose type arguments cannot be inferred from the field,<br/>
    /// and build one by picking each argument in turn, as deep as generic arguments nest.<br/>
    /// The arguments the field does fix are inferred, through variance too, so only the rest are asked for.<br/>
    /// On a field it applies to that field; on a class or an interface, to every field declared as that type.<br/>
    /// Derive from it and override <see cref="Accepts"/> to narrow what the arguments may be, and <see cref="GetDisabledReason"/> to gray out a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public class AllowGenericsAttribute : Attribute {
        /// <summary>
        /// Whether <paramref name="argument"/> may fill <paramref name="parameter"/>, a type parameter of the generic type picked into the field;<br/>
        /// the arguments of generic types nested inside it are bound only by their own constraints.<br/>
        /// <paramref name="argument"/> may be a generic type definition, closed in turn if picked. It runs in the Editor only.
        /// </summary>
        public virtual bool Accepts(Type parameter, Type argument) {
            return true;
        }

        /// <summary>
        /// Why <paramref name="type"/>, a closed type the field can hold, cannot be picked, or null when it can.<br/>
        /// The picker shows the type grayed out with the reason beside it, and refuses it. It runs in the Editor only.
        /// </summary>
        public virtual string GetDisabledReason(Type type) {
            return null;
        }
    }
}
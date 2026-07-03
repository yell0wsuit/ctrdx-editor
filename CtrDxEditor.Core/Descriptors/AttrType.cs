namespace CtrDxEditor.Core.Descriptors
{
    /// <summary>Supported editor input types for object attributes.</summary>
    public enum AttrType
    {
        /// <summary>An integer numeric attribute.</summary>
        Whole,

        /// <summary>A floating-point numeric attribute.</summary>
        Number,

        /// <summary>A Boolean attribute.</summary>
        Bool,

        /// <summary>An attribute selected from a fixed set of string values.</summary>
        Enum,

        /// <summary>An attribute that references another object or object-like id.</summary>
        Ref,

        /// <summary>A free-form text attribute.</summary>
        Text,
    }
}

namespace CtrDxEditor.Core.Document
{
    /// <summary>Reason a proposed ordinary layer name cannot be stored.</summary>
    public enum LayerNameValidationError
    {
        /// <summary>The name is valid.</summary>
        None,

        /// <summary>The trimmed name is empty.</summary>
        Empty,

        /// <summary>The name is reserved for the settings layer.</summary>
        Reserved,

        /// <summary>The name contains characters forbidden by XML.</summary>
        InvalidXml,

        /// <summary>Another ordinary layer already has this name.</summary>
        Duplicate,
    }
}

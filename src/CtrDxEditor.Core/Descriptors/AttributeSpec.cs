namespace CtrDxEditor.Core.Descriptors
{
    /// <summary>One editable attribute of an object type, with its default for new placements.</summary>
    public sealed record AttributeSpec(
        string Name,
        AttrType Type,
        string? Default,
        string[]? EnumValues = null,
        string? RefType = null,
        string? LocalizationName = null);
}

namespace CtrDxEditor.Core.Descriptors
{
    /// <summary>One editable attribute of an object type, with its default for new placements.</summary>
    /// <param name="Name">The XML attribute name, written verbatim.</param>
    /// <param name="Type">Which input the property panel offers for the value.</param>
    /// <param name="Default">
    /// The value written when the object is placed; null omits the attribute, leaving the game's own
    /// default to apply.
    /// </param>
    /// <param name="EnumValues">The permitted values for <see cref="AttrType.Enum"/>; null otherwise.</param>
    /// <param name="RefType">The object type pointed at by an <see cref="AttrType.Ref"/> attribute.</param>
    /// <param name="LocalizationName">
    /// Overrides <paramref name="Name"/> in the <c>Attr.*</c> label and option lookup keys, letting a name
    /// shared across objects carry a distinct label. Defaults to <paramref name="Name"/> when null.
    /// </param>
    public sealed record AttributeSpec(
        string Name,
        AttrType Type,
        string? Default,
        string[]? EnumValues = null,
        string? RefType = null,
        string? LocalizationName = null);
}

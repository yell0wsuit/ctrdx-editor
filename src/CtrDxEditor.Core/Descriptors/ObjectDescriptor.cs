using System.Collections.Generic;

namespace CtrDxEditor.Core.Descriptors
{
    /// <summary>One placeable object type: what the game calls it, and what the editor exposes for it.</summary>
    /// <param name="ElementName">
    /// The level XML tag the game dispatches on, which is authoritative and not always readable. Round-tripped verbatim.
    /// </param>
    /// <param name="DisplayName">The English name shown when no localized string is found.</param>
    /// <param name="Attributes">The attributes the editor turns into property fields; may be empty.</param>
    /// <param name="MaxCount">How many may be placed in one level; <see cref="int.MaxValue"/> means unbounded.</param>
    /// <param name="LocalizationName">
    /// Overrides <paramref name="ElementName"/> in the <c>Object.*</c> lookup key, letting variants share
    /// one string (<c>spike1</c>..<c>spike4</c> all read "Spike") and unreadable tags map to a readable
    /// name. Defaults to <paramref name="ElementName"/> when null.
    /// </param>
    /// <param name="Game">The title this object originates from; the palette groups items under it.</param>
    public sealed record ObjectDescriptor(
        string ElementName,
        string DisplayName,
        IReadOnlyList<AttributeSpec> Attributes,
        int MaxCount,
        string? LocalizationName = null,
        string Game = "Cut the Rope");
}

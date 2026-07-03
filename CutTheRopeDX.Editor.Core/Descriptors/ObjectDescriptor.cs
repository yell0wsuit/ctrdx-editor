using System.Collections.Generic;

namespace CutTheRopeDX.Editor.Core.Descriptors
{
    /// <summary><paramref name="MaxCount"/> of <see cref="int.MaxValue"/> means unbounded.</summary>
    public sealed record ObjectDescriptor(
        string ElementName,
        string DisplayName,
        IReadOnlyList<AttributeSpec> Attributes,
        int MaxCount);
}

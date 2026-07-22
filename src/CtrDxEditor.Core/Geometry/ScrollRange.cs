namespace CtrDxEditor.Core.Geometry
{
    /// <summary>
    /// One axis of scrollbar state derived from the pan bounds. <see cref="Value"/> is always within
    /// <c>[0, Maximum]</c>, so it maps directly onto a control expecting a non-negative scroll offset.
    /// </summary>
    /// <param name="Maximum">The length of the scrollable range in screen pixels.</param>
    /// <param name="Value">The current scroll offset in screen pixels.</param>
    public readonly record struct ScrollRange(double Maximum, double Value);
}

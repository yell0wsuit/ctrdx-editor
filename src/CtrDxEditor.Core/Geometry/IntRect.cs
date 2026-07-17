namespace CtrDxEditor.Core.Geometry
{
    /// <summary>An integer rectangle in atlas pixel coordinates.</summary>
    /// <param name="X">The left edge in atlas pixels, measured from the texture's left.</param>
    /// <param name="Y">The top edge in atlas pixels, measured from the texture's top.</param>
    /// <param name="W">The width in atlas pixels, extending right from <paramref name="X"/>.</param>
    /// <param name="H">The height in atlas pixels, extending down from <paramref name="Y"/>.</param>
    public readonly record struct IntRect(int X, int Y, int W, int H);
}

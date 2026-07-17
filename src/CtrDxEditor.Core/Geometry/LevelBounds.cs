namespace CtrDxEditor.Core.Geometry
{
    /// <summary>Axis-aligned bounds in level space (top-left origin, y down).</summary>
    /// <param name="X">The left edge in level units.</param>
    /// <param name="Y">The top edge in level units.</param>
    /// <param name="W">The width in level units, extending right from <paramref name="X"/>.</param>
    /// <param name="H">The height in level units, extending down from <paramref name="Y"/>.</param>
    public readonly record struct LevelBounds(double X, double Y, double W, double H)
    {
        /// <summary>Returns whether <paramref name="p"/> lies inside or on the edge of the bounds.</summary>
        /// <param name="p">The point to test, in level units.</param>
        /// <returns>True when the point is inside or exactly on an edge.</returns>
        public bool Contains(Vec2 p)
        {
            return p.X >= X && p.X <= X + W && p.Y >= Y && p.Y <= Y + H;
        }
    }
}

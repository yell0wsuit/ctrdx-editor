namespace CtrDxEditor.Core.Geometry
{
    /// <summary>Axis-aligned bounds in level space (top-left origin, y down).</summary>
    public readonly record struct LevelBounds(double X, double Y, double W, double H)
    {
        public bool Contains(Vec2 p)
        {
            return p.X >= X && p.X <= X + W && p.Y >= Y && p.Y <= Y + H;
        }
    }
}

namespace CtrDxEditor.Core.Geometry
{
    /// <summary>A 2D point/vector in level coordinates. No framework dependency.</summary>
    public readonly record struct Vec2(double X, double Y)
    {
        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new(a.X + b.X, a.Y + b.Y);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new(a.X - b.X, a.Y - b.Y);
        }
    }
}

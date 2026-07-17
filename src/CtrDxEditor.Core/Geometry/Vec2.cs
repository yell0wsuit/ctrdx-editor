namespace CtrDxEditor.Core.Geometry
{
    /// <summary>A 2D point/vector in level coordinates. No framework dependency.</summary>
    /// <param name="X">The horizontal coordinate in level units.</param>
    /// <param name="Y">The vertical coordinate in level units, increasing downward.</param>
    public readonly record struct Vec2(double X, double Y)
    {
        /// <summary>Adds two vectors component-wise.</summary>
        /// <param name="a">The left operand.</param>
        /// <param name="b">The right operand.</param>
        /// <returns>The component-wise sum.</returns>
        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>Subtracts two vectors component-wise.</summary>
        /// <param name="a">The vector subtracted from.</param>
        /// <param name="b">The vector to subtract.</param>
        /// <returns>The component-wise difference <paramref name="a"/> minus <paramref name="b"/>.</returns>
        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new(a.X - b.X, a.Y - b.Y);
        }
    }
}

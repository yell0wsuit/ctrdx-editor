namespace CtrDxEditor.Core.Geometry
{
    /// <summary>
    /// View-only zoom/pan. screen = level * Zoom + Pan. Stored level coordinates are never altered
    /// by zoom/pan - this enforces the no-drift contract: both editor and game read the same raw x/y.
    /// </summary>
    public readonly record struct ViewTransform(double Zoom, double PanX, double PanY)
    {
        public static ViewTransform Identity { get; } = new(1.0, 0.0, 0.0);

        public Vec2 LevelToScreen(Vec2 level)
        {
            return new((level.X * Zoom) + PanX, (level.Y * Zoom) + PanY);
        }

        public Vec2 ScreenToLevel(Vec2 screen)
        {
            return new((screen.X - PanX) / Zoom, (screen.Y - PanY) / Zoom);
        }
    }
}

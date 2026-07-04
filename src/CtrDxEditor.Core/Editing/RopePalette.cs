namespace CtrDxEditor.Core.Editing
{
    /// <summary>An RGB color with channels in the 0-1 range. Framework-free.</summary>
    public readonly record struct RopeRgb(double R, double G, double B);

    /// <summary>
    /// The four colors used to draw a rope: two bright base colors and their darker
    /// shades. The renderer ramps each track from shade to base and alternates between
    /// the two tracks to produce the twisted-cord look.
    /// </summary>
    public readonly record struct RopeDrawColors(RopeRgb Base1, RopeRgb Base2, RopeRgb Shade1, RopeRgb Shade2);

    /// <summary>
    /// Rope skin palettes, shading, and the over-stretch red tint. A static reduction
    /// of the game's <c>RopeColorHelper</c> for the editor (the game's highlight state
    /// and per-alpha are not modeled; the editor has neither).
    /// </summary>
    public static class RopePalette
    {
        /// <summary>Number of available rope skins (indices 0..SkinCount-1).</summary>
        public static int SkinCount => 9;

        // The desktop game's stretch trigger is segmentLength > restLength + 7. Applied whole-rope: stretched when distance exceeds
        // length by 7/30.
        private const double StretchThresholdRatio = 7.0 / 30.0;

        /// <summary>True only for the default brown skin (index 0).</summary>
        public static bool IsDefaultSkin(int skin)
        {
            return skin == 0;
        }

        /// <summary>Component-wise linear interpolation between two colors.</summary>
        public static RopeRgb Lerp(RopeRgb a, RopeRgb b, double t)
        {
            return new RopeRgb(
                a.R + ((b.R - a.R) * t),
                a.G + ((b.G - a.G) * t),
                a.B + ((b.B - a.B) * t));
        }

        /// <summary>
        /// Gets the four draw colors for a rope of the given <paramref name="skin"/>,
        /// stretched by the ratio of <paramref name="distance"/> to <paramref name="length"/>.
        /// Mirrors the game's <c>RopeColorHelper.GetDrawColors</c> (alpha = 1, not highlighted).
        /// </summary>
        public static RopeDrawColors GetDrawColors(int skin, double distance, double length)
        {
            int normalizedSkin = skin is >= 0 and < 9 ? skin : 0;
            (RopeRgb primary, RopeRgb secondary) = GetSkin(normalizedSkin);

            bool stretched = length > 0 && distance > length + (StretchThresholdRatio * length);

            // Base colors: when stretched, custom skins draw toward the default brown
            // palette (matches the game); the shade colors keep the skin's own palette.
            (RopeRgb baseSrc1, RopeRgb baseSrc2) = stretched && !IsDefaultSkin(normalizedSkin)
                ? GetSkin(0)
                : (primary, secondary);

            // Default skin (and any stretched skin) is shaded dark to bright; custom skins
            // at rest use full brightness, so shade == base and only alternation shows.
            double darkFactor1 = IsDefaultSkin(normalizedSkin) || stretched ? 0.4 : 1.0;
            double darkFactor2 = IsDefaultSkin(normalizedSkin) || stretched ? 0.45 : 1.0;
            RopeRgb shade1 = new(primary.R * darkFactor1, primary.G * darkFactor1, primary.B * darkFactor1);
            RopeRgb shade2 = new(secondary.R * darkFactor2, secondary.G * darkFactor2, secondary.B * darkFactor2);

            if (stretched)
            {
                // The game reddens via the shade end: shade red *= segmentLength/restLength * 2.
                double redScale = distance / length * 2.0;
                shade1 = shade1 with { R = shade1.R * redScale };
                shade2 = shade2 with { R = shade2.R * redScale };
            }

            return new RopeDrawColors(baseSrc1, baseSrc2, shade1, shade2);
        }

        private static (RopeRgb Primary, RopeRgb Secondary) GetSkin(int skin)
        {
            return skin switch
            {
                1 => (new RopeRgb(0.624, 0.294, 0.114), new RopeRgb(1, 0.627, 0.463)),
                2 => (new RopeRgb(0.404, 0.612, 0.635), new RopeRgb(0.773, 0.898, 0.902)),
                3 => (new RopeRgb(0.757, 0.533, 0), new RopeRgb(0.98, 0.843, 0.2)),
                4 => (new RopeRgb(0.980, 0.243, 0.243), new RopeRgb(0.282, 0.525, 0.153)),
                5 => (new RopeRgb(0.176, 0.318, 0.659), new RopeRgb(1, 1, 1)),
                6 => (new RopeRgb(0.631, 0.957, 1), new RopeRgb(0.996, 0.631, 0.953)),
                7 => (new RopeRgb(1, 0.329, 0.318), new RopeRgb(1, 0.992, 0.941)),
                8 => (new RopeRgb(1, 0.831, 0.404), new RopeRgb(0.251, 0.239, 0.278)),
                _ => (new RopeRgb(0.475, 0.305, 0.185), new RopeRgb(0.6755555555555556, 0.44, 0.27555555555555555)),
            };
        }
    }
}

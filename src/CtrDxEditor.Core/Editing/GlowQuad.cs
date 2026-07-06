namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Geometry for the light-bulb lit-glow halo, kept 1:1 with the game (LightBulb.ApplyGlowScale). The glow
    /// quad is drawn centered on the bulb, its width spanning 1.5x the litRadius on each side — the game's
    /// 1.5f visual multiplier, after its WorldScale (2) and mapScale (3) cancel against the editor's
    /// atlas-pixel-to-level-unit conversion — and its height scaled by the quad's own aspect ratio so the
    /// texture is never distorted.
    /// </summary>
    public static class GlowQuad
    {
        /// <summary>The game's decorative glow multiplier over the semantic lit radius.</summary>
        public const double VisualMultiplier = 1.5;

        /// <summary>
        /// Half the on-canvas glow box in level units for a bulb of <paramref name="litRadius"/>, given the
        /// glow quad's pixel size. Width half is litRadius * 1.5; height half preserves the quad's aspect.
        /// </summary>
        public static (double HalfW, double HalfH) DestRadii(double litRadius, int frameW, int frameH)
        {
            double halfW = litRadius * VisualMultiplier;
            double halfH = frameW > 0 ? halfW * frameH / frameW : halfW;
            return (halfW, halfH);
        }
    }
}

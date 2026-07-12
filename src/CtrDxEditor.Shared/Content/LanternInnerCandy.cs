namespace CtrDxEditor.Content
{
    /// <summary>Which atlas frame holds the lantern's inner candy for a given candy skin.</summary>
    public readonly record struct LanternInnerCandyFrame(string AtlasJsonPath, string AtlasImageBase, int Quad);

    /// <summary>
    /// Resolves the inner-candy frame shown inside an active lantern, matching the game's
    /// <c>Lantern.InitWithPosition</c>: skins 0–2 are baked into <c>obj_lantern</c> (quads 3–5); skins 3+
    /// use quad 10 (<c>frame_10_lantern</c>) of their own candy atlas. Out-of-range skins fall back to 0.
    /// </summary>
    public static class LanternInnerCandy
    {
        private const string LanternJson = "images/obj_lantern.json";
        private const string LanternImageBase = "images/obj_lantern";

        /// <summary>First inner-candy quad in the lantern atlas (skin 0).</summary>
        private const int InnerCandyStartQuad = 3;

        /// <summary>The <c>frame_10_lantern</c> quad index inside a candy atlas.</summary>
        private const int LanternQuadInCandyTexture = 10;

        /// <summary>Resolves the inner-candy frame for a candy skin index.</summary>
        public static LanternInnerCandyFrame Resolve(int skin)
        {
            if (skin < 0 || skin >= CandySkins.Count)
            {
                skin = 0;
            }

            return skin < 3
                ? new LanternInnerCandyFrame(LanternJson, LanternImageBase, InnerCandyStartQuad + skin)
                : new LanternInnerCandyFrame(CandySkins.JsonPath(skin), CandySkins.ResourceBase(skin), LanternQuadInCandyTexture);
        }
    }
}

namespace CtrDxEditor.Content
{
    /// <summary>
    /// Candy skin catalog: index 0 is the default (<c>obj_candy_01_new</c>), 1..51 are
    /// <c>obj_candy_02</c>..<c>obj_candy_52</c>. Mirrors the game's <c>CandySkinHelper.GetCandyResource</c>
    /// so the editor's candy preview matches whichever skin the player has selected.
    /// </summary>
    public static class CandySkins
    {
        /// <summary>Number of candy skins (indices 0..Count-1).</summary>
        public const int Count = 52;

        private const string Dir = "images/candies/";

        /// <summary>
        /// The content-relative atlas base path (no extension) for the given skin index. Out-of-range
        /// indices fall back to the default skin, matching the game's helper.
        /// </summary>
        public static string ResourceBase(int skin)
        {
            if (skin is <= 0 or >= Count)
            {
                return Dir + "obj_candy_01_new";
            }
            // skin 1 -> obj_candy_02, skin 51 -> obj_candy_52.
            return Dir + $"obj_candy_{skin + 1:D2}";
        }

        /// <summary>The atlas JSON path for the given skin index.</summary>
        public static string JsonPath(int skin)
        {
            return ResourceBase(skin) + ".json";
        }
    }
}

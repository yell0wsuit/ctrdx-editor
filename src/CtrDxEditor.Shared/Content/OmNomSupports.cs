namespace CtrDxEditor.Content
{
    /// <summary>
    /// Om Nom's sitting-platform catalog. The platform is the target's back layer, drawn from frame
    /// <c>frame_00NN.png</c> of the shared <c>char_supports</c> atlas; index 0 is the default. The editor
    /// assembles Om Nom as support (back) + character (front), so a platform is just which support frame.
    /// </summary>
    public static class OmNomSupports
    {
        /// <summary>Number of sitting platforms (indices 0..Count-1 = char_supports frame_0000..frame_0016).</summary>
        public const int Count = 17;

        /// <summary>The char_supports frame name for the given platform index.</summary>
        public static string FrameName(int support)
        {
            return $"frame_{support:D4}.png";
        }
    }
}

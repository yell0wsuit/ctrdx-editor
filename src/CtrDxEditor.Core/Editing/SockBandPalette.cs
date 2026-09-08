namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// The color each magic hat group wears on its band, matching DX's <c>SockBandPalette</c>.
    /// </summary>
    /// <remarks>
    /// In the game these are not constants: groups 0 and 1 are baked into the shipped frames, and
    /// everything past them is generated from a seed the player's install draws once and keeps in
    /// <c>hatseed.txt</c>, so no two players see the same hats. The editor cannot know a given player's
    /// seed, so it uses the one the game itself falls back to when there is no seed file - the value
    /// <c>SockBandSeed.FallbackSeed</c> serves to headless runs - which makes the whole generator a
    /// constant function and reduces it to the table below.
    /// <para>
    /// The values were produced by running the game's own <c>SockBandPalette</c> against that seed, not
    /// by eye. Reproducing them means running that generator again; nothing here re-derives them, and
    /// changing the game's generator, its anchors, or its seed will silently leave this table behind.
    /// What the editor is really conveying is which hats pair with which, and that survives the drift.
    /// </para>
    /// </remarks>
    public static class SockBandPalette
    {
        /// <summary>The seed the table below was generated from: the game's own no-seed-file fallback.</summary>
        public const ulong Seed = 0x5CA1AB1E5EEDUL;

        /// <summary>
        /// The band color for a hat group, wrapping back through the generated colors once they run out
        /// exactly as the game's <c>ColorForGroup</c> does, so group 6 repeats group 2 rather than
        /// reaching for a seventh color no player could tell from one already on screen.
        /// </summary>
        /// <param name="group">Teleport group the hat belongs to.</param>
        /// <returns>The tint that group's band wears.</returns>
        public static RopeRgba ColorForGroup(int group)
        {
            return Colors[SockArt.BandSlot(group)];
        }

        /// <summary>
        /// Slots 0 and 1 are the colors the shipped art bakes into the group 0 and group 1 frames; the
        /// editor never tints with them (those hats draw their own art) but keeps them so this answers
        /// for every group the way the game's own lookup does. Slots 2 onwards are the generated colors.
        /// </summary>
        private static readonly RopeRgba[] Colors =
        [
            new(1.0, 0.1882353, 0.078431375, 1.0),      // group 0: #FF3014, baked into obj_hat frame 0
            new(0.5294118, 1.0, 0.0, 1.0),              // group 1: #87FF00, baked into obj_hat frame 1
            new(0.15217209, 0.9876165, 0.9962806, 1.0), // group 2: #27FCFE
            new(0.8580622, 0.023850502, 0.9186806, 1.0),// group 3: #DB06EA
            new(0.8871647, 0.7004546, 0.2240868, 1.0),  // group 4: #E2B339
            new(0.15807183, 0.60179967, 0.34000343, 1.0),// group 5: #289957
        ];
    }
}

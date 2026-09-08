using System;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Which art a magic hat draws for its transporter group, ported from DX's <c>SockArt</c>.
    /// </summary>
    /// <remarks>
    /// The shipped art bakes a band color into one frame per authored group. Past those, a group draws
    /// one of the same frames again and wears a band generated from the player's seed over it, which is
    /// what lets a level use as many hat pairs as it likes. Two consequences the editor has to reproduce:
    /// a generated-band group draws frame <see cref="PatternFor"/>, not "the grouped frame"; and it falls
    /// back to the magic hat during the Christmas event, because the seasonal art draws two socks and
    /// stops there.
    /// </remarks>
    public static class SockArt
    {
        /// <summary>How many groups the shipped art already bakes a color into.</summary>
        public const int AuthoredCount = 2;

        /// <summary>How many band colors are generated before the palette repeats.</summary>
        public const int GeneratedCount = 4;

        /// <summary>
        /// A group as the art can use it. Level XML is data, and a negative group would otherwise index
        /// off the front of every lookup here.
        /// </summary>
        /// <param name="group">Teleport group as the level authored it.</param>
        /// <returns>The group, never below zero.</returns>
        public static int NormalizeGroup(int group)
        {
            return Math.Max(group, 0);
        }

        /// <summary>Whether a hat of this group wears a band generated from the seed.</summary>
        /// <param name="group">Teleport group the hat belongs to.</param>
        /// <returns><see langword="true"/> when the shipped art bakes no color for this group.</returns>
        public static bool WearsGeneratedBand(int group)
        {
            return NormalizeGroup(group) >= AuthoredCount;
        }

        /// <summary>The base frame, and the band pattern authored over it, for this group.</summary>
        /// <param name="group">Teleport group the hat belongs to.</param>
        /// <returns>Zero-based pattern index.</returns>
        public static int PatternFor(int group)
        {
            return NormalizeGroup(group) % AuthoredCount;
        }

        /// <summary>
        /// The palette slot a generated-band group draws its color from, which is the group itself until
        /// the generated colors run out and then wraps back through them.
        /// </summary>
        /// <remarks>
        /// The one number that distinguishes one generated hat's art from another: the color comes from
        /// the slot, and so does the base frame, since <see cref="GeneratedCount"/> is a whole number of
        /// <see cref="AuthoredCount"/> patterns. That is why four sprite keys cover every group from
        /// <see cref="AuthoredCount"/> up.
        /// </remarks>
        /// <param name="group">Teleport group the hat belongs to.</param>
        /// <returns>The group itself below <see cref="AuthoredCount"/>, else a slot in <c>[AuthoredCount, AuthoredCount + GeneratedCount)</c>.</returns>
        public static int BandSlot(int group)
        {
            int normalized = NormalizeGroup(group);
            return normalized < AuthoredCount
                ? normalized
                : AuthoredCount + ((normalized - AuthoredCount) % GeneratedCount);
        }
    }
}

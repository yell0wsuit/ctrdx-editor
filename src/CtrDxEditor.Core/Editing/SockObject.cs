using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for DX's magic-hat teleporter, stored as a <c>sock</c> XML element.</summary>
    public static class SockObject
    {
        /// <summary>Prefix of the sprite keys for hats wearing a generated (rather than baked) band.</summary>
        public const string BandKeyPrefix = "sock_band_";

        /// <summary>The sprite key for a hat drawing the generated band of one palette slot.</summary>
        /// <param name="slot">Palette slot, as <see cref="SockArt.BandSlot"/> resolves it.</param>
        /// <returns>The key naming that slot's composited art.</returns>
        public static string BandKey(int slot)
        {
            return BandKeyPrefix + slot.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Returns the visual key selected by the Christmas event and transporter group.</summary>
        /// <remarks>
        /// Follows <see cref="SockArt"/>: the two authored groups pick a baked frame (and the Christmas
        /// sock during the event), while a group past them wears a generated band, which exists only over
        /// the magic hat - so those keys have no Christmas variant, exactly as <c>SockArt.TextureFor</c>
        /// falls back for them.
        /// </remarks>
        /// <param name="obj">Magic-hat level object.</param>
        /// <param name="isXmas">Whether DX's Christmas event is active.</param>
        /// <returns>A key choosing the atlas, the group quad, and any generated band over it.</returns>
        public static string SpriteKey(LevelObject obj, bool isXmas)
        {
            // A group the level did not author, or authored unparseably, reads as 0 - the game's
            // ParseIntOrZero - and a negative one is clamped by SockArt before it reaches the art.
            _ = TryParseGroup(obj.GetAttr("group"), out int group);

            return (SockArt.WearsGeneratedBand(group), isXmas, SockArt.PatternFor(group)) switch
            {
                (true, _, _) => BandKey(SockArt.BandSlot(group)),
                (false, false, 0) => "sock",
                (false, false, _) => "sock_grouped",
                (false, true, 0) => "sock_xmas",
                (false, true, _) => "sock_xmas_grouped",
            };
        }

        /// <summary>
        /// Returns the group number to draw on a grouped hat, or null when no label is needed.
        /// Grouped hats (nonzero group) all share one sprite, so they are labeled only once the level
        /// holds at least two distinct nonzero groups; group-0 (plain) hats are never labeled.
        /// </summary>
        /// <param name="obj">The hat being drawn.</param>
        /// <param name="objects">All level objects.</param>
        public static string? GroupLabel(LevelObject obj, IEnumerable<LevelObject> objects)
        {
            if (!TryParseGroup(obj.GetAttr("group"), out int group) || group == 0)
            {
                return null;
            }

            HashSet<int> distinctNonzero = [];
            foreach (LevelObject other in objects)
            {
                if (other.Type == "sock" && TryParseGroup(other.GetAttr("group"), out int g) && g != 0)
                {
                    _ = distinctNonzero.Add(g);
                }
            }

            return distinctNonzero.Count >= 2 ? group.ToString(CultureInfo.InvariantCulture) : null;
        }

        private static bool TryParseGroup(string? value, out int group)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out group);
        }
    }
}

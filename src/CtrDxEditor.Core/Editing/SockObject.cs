using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for DX's magic-hat teleporter, stored as a <c>sock</c> XML element.</summary>
    public static class SockObject
    {
        /// <summary>Returns the visual key selected by the Christmas event and transporter group.</summary>
        /// <param name="obj">Magic-hat level object.</param>
        /// <param name="isXmas">Whether DX's Christmas event is active.</param>
        /// <returns>A key choosing the normal or Christmas atlas and group quad.</returns>
        public static string SpriteKey(LevelObject obj, bool isXmas)
        {
            bool grouped = int.TryParse(
                obj.GetAttr("group"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int group) && group != 0;

            return (isXmas, grouped) switch
            {
                (false, false) => "sock",
                (false, true) => "sock_grouped",
                (true, false) => "sock_xmas",
                (true, true) => "sock_xmas_grouped",
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

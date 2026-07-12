using System.Globalization;
using System.Text.RegularExpressions;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for regular spike objects, which are stored as spike1 through spike4 in game XML.</summary>
    public static partial class SpikeObject
    {
        private static readonly string[] Sizes = ["1", "2", "3", "4"];

        /// <summary>Whether <paramref name="element"/> is one of the regular spike XML elements.</summary>
        public static bool IsSpike(string element)
        {
            return SpikeElementRegex().IsMatch(element);
        }

        /// <summary>The spike size from its size attribute or element suffix, clamped to supported values.</summary>
        public static string Size(LevelObject obj)
        {
            string? attr = obj.GetAttr("size");
            return IsValidSize(attr)
                ? attr!
                : IsSpike(obj.Type) ? obj.Type[^1].ToString(CultureInfo.InvariantCulture) : "1";
        }

        /// <summary>Changes the spike size and renames the backing XML element to the matching spikeN name.</summary>
        public static void SetSize(LevelObject obj, string? size)
        {
            if (!IsValidSize(size))
            {
                return;
            }

            obj.Element.Name = $"spike{size}";
            obj.SetAttr("size", size!);
        }

        /// <summary>
        /// Renames the element to spikeN so the tag matches its authoritative size attribute.
        /// The game reads size only from the attribute, so this is behavior-preserving. Elements
        /// without a valid size attribute (bare spikes, electro, out-of-range) are left untouched.
        /// </summary>
        public static void NormalizeElementName(LevelObject obj)
        {
            if (!IsSpike(obj.Type))
            {
                return;
            }

            string? size = obj.GetAttr("size");
            if (IsValidSize(size) && obj.Type != $"spike{size}")
            {
                obj.Element.Name = $"spike{size}";
            }
        }

        /// <summary>Whether this spike uses rotatable spike state. Group 0 is rotatable but has no embedded button.</summary>
        public static bool IsToggled(LevelObject obj)
        {
            string? value = obj.GetAttr("toggled");
            return value is "0" or "1" or "2";
        }

        /// <summary>Sets or clears spike rotation grouping.</summary>
        public static void SetToggled(LevelObject obj, bool toggled)
        {
            obj.SetAttr("toggled", toggled ? Group(obj) : "false");
        }

        /// <summary>The current spike toggle group, defaulting to group 1.</summary>
        public static string Group(LevelObject obj)
        {
            string? value = obj.GetAttr("toggled");
            return value is "0" or "2" ? value : "1";
        }

        /// <summary>Sets the spike toggle group to one of the game-supported values.</summary>
        public static void SetGroup(LevelObject obj, string? group)
        {
            obj.SetAttr("toggled", group is "0" or "2" ? group : "1");
        }

        /// <summary>Sprite key for the spike's current toggle state.</summary>
        public static string SpriteKey(LevelObject obj)
        {
            return IsToggled(obj) ? $"{obj.Type}_toggled_{Group(obj)}" : obj.Type;
        }

        /// <summary>Whether the supplied size is one of 1, 2, 3, or 4.</summary>
        public static bool IsValidSize(string? size)
        {
            return size is not null && System.Array.IndexOf(Sizes, size) >= 0;
        }

        [GeneratedRegex("^spike[1-4]$")]
        private static partial Regex SpikeElementRegex();
    }
}

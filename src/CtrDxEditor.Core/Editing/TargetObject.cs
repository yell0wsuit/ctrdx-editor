using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Helpers for Om Nom's skin, stored as the target's <c>targetType</c> attribute. The game resolves it
    /// in <c>OmNomSkinRegistry.ResolveTargetSkinIndex</c>: 1..<see cref="SkinCount"/> pick skin slot
    /// <c>targetType - 1</c> (slot 0 is the classic sprite skin, the rest come from the skin manifest), while
    /// 0, a negative, an unparseable value, or anything past the end defers to the player's own skin choice.
    /// </summary>
    public static class TargetObject
    {
        /// <summary>
        /// The value meaning "whichever skin the player has selected", and what the editor shows for any
        /// value the game would not resolve to a skin.
        /// </summary>
        public const string PlayerChoice = "0";

        /// <summary>
        /// Highest usable <c>targetType</c>, matching the game's skin count: the classic skin plus the
        /// fifteen skins in <c>om_nom_skins.json</c>. Raise this (and add the matching
        /// <c>Attr.targetType.N</c> strings) when the game ships another skin.
        /// </summary>
        public const int SkinCount = 16;

        /// <summary>Whether an element is the Om Nom target.</summary>
        public static bool IsTarget(string element)
        {
            return element == "target";
        }

        /// <summary>
        /// The skin to show in the properties panel: the attribute when it names a real skin, and
        /// <see cref="PlayerChoice"/> for anything the game would fall back on. Falling back here rather than
        /// rewriting the XML keeps an out-of-range value from a hand-edited level intact until it is edited.
        /// </summary>
        public static string Skin(LevelObject obj)
        {
            string? attribute = obj.GetAttr("targetType");
            return int.TryParse(attribute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                && value >= 1
                && value <= SkinCount
                ? value.ToString(CultureInfo.InvariantCulture)
                : PlayerChoice;
        }

        /// <summary>
        /// Writes the chosen skin, removing the attribute for <see cref="PlayerChoice"/> so a level that
        /// leaves the skin to the player carries no attribute at all. Values outside the skin range are
        /// ignored rather than written.
        /// </summary>
        public static void SetSkin(LevelObject obj, string? skin)
        {
            if (skin == PlayerChoice)
            {
                obj.RemoveAttr("targetType");
                return;
            }

            if (int.TryParse(skin, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                && value >= 1
                && value <= SkinCount)
            {
                obj.SetAttr("targetType", value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}

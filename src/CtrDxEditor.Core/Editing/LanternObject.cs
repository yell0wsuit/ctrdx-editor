using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Lantern state helpers shared by the renderer, property panel, and validator.</summary>
    public static class LanternObject
    {
        /// <summary>The lantern XML element name.</summary>
        public const string Element = "lantern";

        /// <summary>Sprite key for the idle (empty) lantern body.</summary>
        public const string IdleSpriteKey = "lantern";

        /// <summary>Sprite key for the active (candy-holding) lantern body.</summary>
        public const string ActiveSpriteKey = "lantern_active";

        /// <summary>Whether the element name is a lantern.</summary>
        /// <param name="type">XML element name to inspect.</param>
        /// <returns><see langword="true"/> when <paramref name="type"/> is <c>lantern</c>.</returns>
        public static bool IsLantern(string? type)
        {
            return type == Element;
        }

        /// <summary>Whether this lantern currently holds the candy.</summary>
        /// <param name="obj">Level object whose capture state is read.</param>
        /// <returns><see langword="true"/> only for a lantern with <c>candyCaptured="true"</c>.</returns>
        public static bool IsCaptured(LevelObject obj)
        {
            return IsLantern(obj.Type)
                && bool.TryParse(obj.GetAttr("candyCaptured"), out bool captured)
                && captured;
        }

        /// <summary>Whether any lantern in the level holds the candy.</summary>
        /// <param name="objects">Level objects to scan.</param>
        /// <returns><see langword="true"/> when at least one captured lantern is present.</returns>
        public static bool AnyCaptured(IReadOnlyList<LevelObject> objects)
        {
            return objects.Any(IsCaptured);
        }

        /// <summary>The state-dependent sprite key for a lantern's body.</summary>
        /// <param name="obj">Lantern whose capture state selects the sprite.</param>
        /// <returns>The active sprite key when captured; otherwise the idle sprite key.</returns>
        public static string SpriteKey(LevelObject obj)
        {
            return IsCaptured(obj) ? ActiveSpriteKey : IdleSpriteKey;
        }

        /// <summary>
        /// Whether this candy is the primary candy — the first <c>&lt;candy&gt;</c> in document order,
        /// which a captured lantern pulls in. Candies with id 1+ stay free and are never captured.
        /// </summary>
        /// <param name="candy">Candidate candy object.</param>
        /// <param name="objects">All level objects in document order.</param>
        /// <returns><see langword="true"/> when <paramref name="candy"/> is the first candy object.</returns>
        public static bool IsPrimaryCandy(LevelObject candy, IReadOnlyList<LevelObject> objects)
        {
            if (candy.Type != "candy")
            {
                return false;
            }

            LevelObject? first = objects.FirstOrDefault(o => o.Type == "candy");
            return ReferenceEquals(first, candy);
        }
    }
}

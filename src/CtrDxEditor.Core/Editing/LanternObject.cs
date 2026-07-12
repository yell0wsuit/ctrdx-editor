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
        public static bool IsLantern(string? type)
        {
            return type == Element;
        }

        /// <summary>Whether this lantern currently holds the candy.</summary>
        public static bool IsCaptured(LevelObject obj)
        {
            return IsLantern(obj.Type)
                && bool.TryParse(obj.GetAttr("candyCaptured"), out bool captured)
                && captured;
        }

        /// <summary>Whether any lantern in the level holds the candy.</summary>
        public static bool AnyCaptured(IReadOnlyList<LevelObject> objects)
        {
            return objects.Any(IsCaptured);
        }

        /// <summary>The state-dependent sprite key for a lantern's body.</summary>
        public static string SpriteKey(LevelObject obj)
        {
            return IsCaptured(obj) ? ActiveSpriteKey : IdleSpriteKey;
        }

        /// <summary>
        /// Whether this candy is the primary candy — the first <c>&lt;candy&gt;</c> in document order,
        /// which a captured lantern pulls in. Candies with id 1+ stay free and are never captured.
        /// </summary>
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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Level-aware rules for object defaults and editable attribute availability.</summary>
    public static class LevelObjectPolicy
    {
        /// <summary>Applies defaults that depend on the active level settings.</summary>
        public static void ApplyDefaults(LevelObject obj, LevelDocument document)
        {
            if (document.TwoParts && obj.Type == "grab" && obj.GetAttr("part") is null)
            {
                obj.SetAttr("part", "L");
            }

            // Non-twoParts candies are keyed 0-based; the first placed becomes primary "0" and an
            // unnumbered primary is backfilled when a second candy arrives. Called before the new
            // object is added, so document.Objects holds only the prior candies.
            if (obj.Type == "candy" && !document.TwoParts)
            {
                List<LevelObject> candies = [.. document.Objects.Where(o => o.Type == "candy")];
                if (candies.Count >= 1 && candies[0].GetAttr("candyNumber") is null)
                {
                    candies[0].SetAttr("candyNumber", "0");
                }
                obj.SetAttr("candyNumber", KeyNumbering.NextKey(candies.Select(c => c.GetAttr("candyNumber"))));
            }

            // Bulbs are keyed 0-based; legacy "first" keys are ignored (never rewritten) so a new
            // bulb alongside them simply takes "0".
            if (obj.Type is "lightBulb" or "lightbulb")
            {
                IEnumerable<string?> keys = document.Objects
                    .Where(o => o.Type is "lightBulb" or "lightbulb")
                    .Select(o => o.GetAttr("bulbNumber"));
                obj.SetAttr("bulbNumber", KeyNumbering.NextKey(keys));
            }

            // Magic hats teleport in pairs; a new hat completes an open pair or starts a fresh group.
            if (obj.Type == "sock")
            {
                obj.SetAttr("group", SockGrouping.NextGroup(
                    document.Objects.Where(o => o.Type == "sock").Select(o => o.GetAttr("group"))));
            }
        }

        /// <summary>
        /// Reassigns hidden candy and bulb ids from zero in object order, updating grab references
        /// that pointed at matching legacy keys.
        /// </summary>
        public static void NormalizeBindingKeys(LevelDocument document)
        {
            IReadOnlyList<LevelObject> objects = document.Objects;
            Dictionary<string, string> candyMap = NormalizeObjects(
                objects.Where(o => o.Type == "candy"),
                "candyNumber");
            Dictionary<string, string> bulbMap = NormalizeObjects(
                objects.Where(o => o.Type is "lightBulb" or "lightbulb"),
                "bulbNumber");

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                if (IsTrue(grab.GetAttr("bindBulb")))
                {
                    Retarget(grab, "bulbNumber", bulbMap);
                }
                else if (!document.TwoParts)
                {
                    Retarget(grab, "candyNumber", candyMap);
                }
            }
        }

        /// <summary>Returns whether an object attribute should be exposed for editing in this level.</summary>
        public static bool IsAttributeVisible(string element, string attribute, LevelDocument document)
        {
            _ = document;

            // `part` is subsumed by the grab "Attach to" binding control.
            if (element == "grab" && attribute == "part")
            {
                return false;
            }

            // Electro requires size="5" in XML for the game, but it is not user-editable.
            if (element == "electro" && attribute == "size")
            {
                return false;
            }

            // Binding keys are authored internally and selected through grab "Attach to".
            return (element != "candy" || attribute != "candyNumber")
                && (element is not ("lightBulb" or "lightbulb") || attribute != "bulbNumber");
        }

        private static Dictionary<string, string> NormalizeObjects(IEnumerable<LevelObject> objects, string attribute)
        {
            Dictionary<string, string> map = [];
            int key = 0;
            foreach (LevelObject obj in objects)
            {
                string next = key.ToString(CultureInfo.InvariantCulture);
                if (obj.GetAttr(attribute) is { } old)
                {
                    _ = map.TryAdd(old.Trim(), next);
                }
                obj.SetAttr(attribute, next);
                key++;
            }
            return map;
        }

        private static void Retarget(LevelObject grab, string attribute, Dictionary<string, string> keyMap)
        {
            if (grab.GetAttr(attribute) is { } old && keyMap.TryGetValue(old.Trim(), out string? next))
            {
                grab.SetAttr(attribute, next);
            }
        }

        private static bool IsTrue(string? v)
        {
            return bool.TryParse(v, out bool b) && b;
        }
    }
}

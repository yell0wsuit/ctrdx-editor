using System.Collections.Generic;
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
        }

        /// <summary>Returns whether an object attribute should be exposed for editing in this level.</summary>
        public static bool IsAttributeVisible(string element, string attribute, LevelDocument document)
        {
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

            // The candyNumber key is only meaningful outside split-candy levels.
            return element != "candy" || attribute != "candyNumber" || !document.TwoParts;
        }
    }
}

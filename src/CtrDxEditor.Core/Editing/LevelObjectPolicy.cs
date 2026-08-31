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
            // Tutorials are authored in English, and the game only displays them when locale matches.
            if (TutorialObject.IsImage(obj.Type) || TutorialObject.IsText(obj.Type))
            {
                TutorialObject.EnsureEnglishLocale(obj);
            }

            // A freshly placed tutorial text starts in auto-width mode so its box grows with the text.
            if (TutorialObject.IsText(obj.Type))
            {
                TutorialObject.SetAutoWidth(obj, true);
            }

            if (document.TwoParts && obj.Type == "grab" && obj.GetAttr("part") is null)
            {
                obj.SetAttr("part", "L");
            }

            // Non-twoParts candies are keyed 0-based; the first placed becomes primary "0" and an
            // unnumbered primary is backfilled when a second candy arrives. Called before the new
            // object is added, so document.AllObjects holds only the prior candies.
            if (obj.Type == "candy" && !document.TwoParts)
            {
                List<LevelObject> candies = [.. document.AllObjects.Where(o => o.Type == "candy")];
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
                IEnumerable<string?> keys = document.AllObjects
                    .Where(o => o.Type is "lightBulb" or "lightbulb")
                    .Select(o => o.GetAttr("bulbNumber"));
                obj.SetAttr("bulbNumber", KeyNumbering.NextKey(keys));
            }

            // Axes are keyed 0-based like bulbs, so a grab can name one through "Attach to" without
            // anyone typing a key. LoadAxe reads a missing axeNumber as "", which would silently
            // collide across axes, so every placed axe gets an explicit key.
            if (AxeBinding.IsAxe(obj))
            {
                IEnumerable<string?> axeKeys = document.AllObjects
                    .Where(AxeBinding.IsAxe)
                    .Select(o => o.GetAttr(AxeBinding.KeyAttribute));
                obj.SetAttr(AxeBinding.KeyAttribute, KeyNumbering.NextKey(axeKeys));
            }

            // Magic hats teleport in pairs; a new hat completes an open pair or starts a fresh group.
            if (obj.Type == "sock")
            {
                obj.SetAttr("group", SockGrouping.NextGroup(
                    document.AllObjects.Where(o => o.Type == "sock").Select(o => o.GetAttr("group"))));
            }

            // Mice activate in ascending index order; a new mouse takes one past the highest existing
            // index (max+1, not count+1, so it stays unique after a mouse is deleted). The game itself
            // falls back to mice.Count+1 only when index is absent, so an explicit value never collides.
            if (obj.Type == "gap")
            {
                int max = document.AllObjects
                    .Where(o => o.Type == "gap")
                    .Select(o => int.TryParse(o.GetAttr("index"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                obj.SetAttr("index", (max + 1).ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Aligns each spike/bouncer element name with its authoritative <c>size</c> attribute
        /// (e.g. <c>spike2 size="3"</c> becomes <c>spike3</c>). The game reads size only from the
        /// attribute, so this is behavior-preserving; electro, missing-size, and out-of-range
        /// sizes are left untouched. Returns whether any element was renamed.
        /// </summary>
        public static bool NormalizeSizedElements(LevelDocument document)
        {
            bool changed = false;
            foreach (LevelObject obj in document.AllObjects)
            {
                string before = obj.Type;
                SpikeObject.NormalizeElementName(obj);
                BouncerObject.NormalizeElementName(obj);
                changed |= obj.Type != before;
            }

            return changed;
        }

        /// <summary>
        /// Renames the legacy <c>mouse</c> tag to <c>gap</c>. The game dispatches both tags to the
        /// same LoadMouse loader (GameScene.LoadObjects), so this is behavior-preserving and lets the
        /// editor treat the mouse as a single registered object. Returns whether any tag was renamed.
        /// </summary>
        public static bool NormalizeMouseAlias(LevelDocument document)
        {
            bool changed = false;
            foreach (LevelObject obj in document.AllObjects)
            {
                if (obj.Type == "mouse")
                {
                    obj.Element.Name = "gap";
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Drops fractional parts from object <c>x</c>/<c>y</c> coordinates and the gameDesign
        /// <c>mapOffsetX</c>/<c>mapOffsetY</c>, truncating toward zero to match the game's
        /// <c>ParseCoordinateIntOrZero</c> (e.g. <c>"-12.9"</c> becomes <c>"-12"</c>). Integer,
        /// missing, and unparseable or out-of-range values are left untouched. Returns whether any
        /// value was rewritten.
        /// </summary>
        public static bool DropCoordinateDecimals(LevelDocument document)
        {
            bool changed = false;
            foreach (LevelObject obj in document.AllObjects)
            {
                changed |= TruncateCoordinate(obj, "x");
                changed |= TruncateCoordinate(obj, "y");
            }

            if (document.GameDesignElement is { } gameDesign)
            {
                LevelObject design = new(gameDesign);
                changed |= TruncateCoordinate(design, "mapOffsetX");
                changed |= TruncateCoordinate(design, "mapOffsetY");
            }

            return changed;
        }

        private static bool TruncateCoordinate(LevelObject obj, string attribute)
        {
            if (obj.GetAttr(attribute) is not { } raw
                || !decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
                || value < int.MinValue
                || value > int.MaxValue)
            {
                return false;
            }

            string truncated = decimal.ToInt32(decimal.Truncate(value)).ToString(CultureInfo.InvariantCulture);
            if (truncated == raw)
            {
                return false;
            }

            obj.SetAttr(attribute, truncated);
            return true;
        }

        /// <summary>
        /// Reassigns hidden candy, bulb, and axe ids from zero in object order, updating grab references
        /// that pointed at matching legacy keys.
        /// </summary>
        public static void NormalizeBindingKeys(LevelDocument document)
        {
            IReadOnlyList<LevelObject> objects = document.AllObjects;
            Dictionary<string, string> candyMap = NormalizeObjects(
                objects.Where(o => o.Type == "candy"),
                "candyNumber");
            Dictionary<string, string> bulbMap = NormalizeObjects(
                objects.Where(o => o.Type is "lightBulb" or "lightbulb"),
                "bulbNumber");
            Dictionary<string, string> axeMap = NormalizeObjects(
                objects.Where(AxeBinding.IsAxe),
                AxeBinding.KeyAttribute);

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                if (IsTrue(grab.GetAttr("bindBulb")))
                {
                    Retarget(grab, "bulbNumber", bulbMap);
                }
                else if (AxeBinding.RequestedKey(grab) is { } axeKey && axeMap.ContainsKey(axeKey.Trim()))
                {
                    // An imported axed="true" grab keeps its axe key in candyNumber, so remap whichever
                    // attribute is actually holding it - against the axe map either way. A key no axe
                    // answers to is left to the candy branch below, where the game's fallback puts it.
                    Retarget(
                        grab,
                        grab.GetAttr(AxeBinding.KeyAttribute) is not null ? AxeBinding.KeyAttribute : "candyNumber",
                        axeMap);
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

            // The mouse's activation index is auto-numbered on placement and shown as an on-canvas
            // label, so it is not exposed as an editable field.
            if (element == "gap" && attribute == "index")
            {
                return false;
            }

            // Binding keys are authored internally and selected through grab "Attach to".
            return (element != "candy" || attribute != "candyNumber")
                && (element is not ("lightBulb" or "lightbulb") || attribute != "bulbNumber")
                && (element != AxeBinding.Element || attribute != AxeBinding.KeyAttribute);
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

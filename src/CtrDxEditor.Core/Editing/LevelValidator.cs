using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Non-blocking structural checks for a level. Returns human-readable warnings describing
    /// states that make the level crash or play incorrectly in Cut the Rope: DX.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>Returns the level's structural warnings, or an empty list when it looks playable.</summary>
        public static IReadOnlyList<string> Validate(LevelDocument document)
        {
            List<string> warnings = [];

            IReadOnlyList<LevelObject> objects = document.Objects;
            bool HasType(string type)
            {
                return objects.Any(o => o.Type == type);
            }

            bool hasCandy = HasType("candy");
            bool hasLeft = HasType("candyL");
            bool hasRight = HasType("candyR");

            if (document.TwoParts)
            {
                if (!hasLeft || !hasRight)
                {
                    warnings.Add("Two-part level is missing a candy half - DX will crash without both candyL and candyR.");
                }
                if (hasCandy)
                {
                    warnings.Add("Two-part level shouldn't contain a plain candy.");
                }
            }
            else
            {
                if (hasLeft || hasRight)
                {
                    warnings.Add("Single-candy level shouldn't contain candyL/candyR.");
                }
            }

            if (document.NightLevel && !HasType("lightBulb"))
            {
                warnings.Add("Night level has no light bulb; it will render fully dark.");
            }

            if (!hasCandy && !hasLeft && !hasRight)
            {
                warnings.Add("Level has no candy.");
            }

            if (!HasType("target"))
            {
                warnings.Add("Level has no Om Nom (target).");
            }

            // Sizes below the smallest real level (320x480) are almost certainly a hand-edit mistake.
            // Warn only - auto-defaulting the size would break the lossless XML round-trip.
            if (document.Width < 320 || document.Height < 480)
            {
                warnings.Add("Level resolution is smaller than 320x480.");
            }

            // Duplicate candy keys collide under string-identity matching.
            List<string> candyKeys =
            [
                .. objects
                .Where(o => o.Type == "candy")
                .Select(o => o.GetAttr("candyNumber"))
                .Where(k => k is not null)
                .Select(k => k!.Trim())
            ];
            if (candyKeys.Count != candyKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                warnings.Add("Duplicate candyNumber values found; grabs cannot tell those candies apart.");
            }

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                string? candyNumber = grab.GetAttr("candyNumber");
                if (candyNumber is not null
                    && !candyKeys.Any(k => string.Equals(k, candyNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add($"A grab references candyNumber '{candyNumber}', which no candy has; it will bind to the primary candy.");
                }

                if (IsTrueAttr(grab, "bindBulb"))
                {
                    string? bulbNumber = grab.GetAttr("bulbNumber");
                    bool anyBulbMatches = objects.Any(o =>
                        (o.Type is "lightBulb" or "lightbulb")
                        && bulbNumber is not null
                        && string.Equals(o.GetAttr("bulbNumber")?.Trim(), bulbNumber.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (!anyBulbMatches)
                    {
                        warnings.Add($"A grab binds to bulbNumber '{bulbNumber}', which no light bulb has.");
                    }
                }
            }

            return warnings;
        }

        private static bool IsTrueAttr(LevelObject obj, string name)
        {
            return bool.TryParse(obj.GetAttr(name), out bool b) && b;
        }
    }
}

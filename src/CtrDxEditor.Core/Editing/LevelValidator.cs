using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Non-blocking structural checks for a level. Returns keyed warnings describing states that
    /// make the level crash or play incorrectly in Cut the Rope: DX; the UI layer localizes them.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>Returns the level's structural warnings, or an empty list when it looks playable.</summary>
        public static IReadOnlyList<LevelWarning> Validate(LevelDocument document)
        {
            List<LevelWarning> warnings = [];

            IReadOnlyList<LevelObject> objects = document.AllObjects;
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
                    warnings.Add(new LevelWarning("Validation.TwoPartMissingHalf"));
                }
                if (hasCandy)
                {
                    warnings.Add(new LevelWarning("Validation.TwoPartHasPlainCandy"));
                }
            }
            else
            {
                if (hasLeft || hasRight)
                {
                    warnings.Add(new LevelWarning("Validation.SingleCandyHasHalves"));
                }
            }

            if (document.NightLevel && !HasType("lightBulb"))
            {
                warnings.Add(new LevelWarning("Validation.NightNoBulb"));
            }

            bool capturedLantern = document.AllObjects.Any(LanternObject.IsCaptured);
            if (!hasCandy && !hasLeft && !hasRight && !capturedLantern)
            {
                warnings.Add(new LevelWarning("Validation.NoCandy"));
            }

            if (!HasType("target"))
            {
                warnings.Add(new LevelWarning("Validation.NoTarget"));
            }

            // Sizes below the smallest real level (320x480) are almost certainly a hand-edit mistake.
            // Warn only - auto-defaulting the size would break the lossless XML round-trip.
            if (document.Width < 320 || document.Height < 480)
            {
                warnings.Add(new LevelWarning("Validation.ResolutionTooSmall"));
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
                warnings.Add(new LevelWarning("Validation.DuplicateCandyNumber"));
            }

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                string? candyNumber = grab.GetAttr("candyNumber");
                if (candyNumber is not null
                    && !candyKeys.Any(k => string.Equals(k, candyNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add(new LevelWarning("Validation.GrabUnmatchedCandyNumber", candyNumber));
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
                        warnings.Add(new LevelWarning("Validation.GrabUnmatchedBulbNumber", bulbNumber ?? string.Empty));
                    }
                }
            }

            foreach (LevelObject ghost in objects.Where(o => o.Type == "ghost"))
            {
                if (GhostStates.IsIdleOnly(ghost))
                {
                    warnings.Add(new LevelWarning("Validation.GhostIdle"));
                }
            }

            foreach (LevelObject candy in HazardOverlap.CandiesInHazards(document))
            {
                warnings.Add(new LevelWarning("Validation.CandyInHazard", CandyLabel(candy)));
            }

            foreach (LevelObject candy in MouthOverlap.CandiesOnMouth(document))
            {
                warnings.Add(new LevelWarning("Validation.CandyOnMouth", CandyLabel(candy)));
            }

            return warnings;
        }

        private static bool IsTrueAttr(LevelObject obj, string name)
        {
            return bool.TryParse(obj.GetAttr(name), out bool b) && b;
        }

        private static string CandyLabel(LevelObject candy)
        {
            string? number = candy.GetAttr("candyNumber");
            return !string.IsNullOrWhiteSpace(number)
                ? number.Trim()
                : candy.Type switch
                {
                    "candyL" => "L",
                    "candyR" => "R",
                    _ => "?",
                };
        }
    }
}

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

            if (document.SettingsLayerCount > 1)
            {
                warnings.Add(new LevelWarning("Validation.DuplicateSettingsLayer"));
            }

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

            List<LevelObject> axes = [.. objects.Where(AxeBinding.IsAxe)];
            bool BindsToAnAxe(LevelObject grab)
            {
                return AxeBinding.RequestedKey(grab) is { } key
                    && axes.Any(a => AxeBinding.KeyEquals(AxeBinding.KeyOf(a), key));
            }

            // A chain survives every cut but the axe's, so a level whose chains have no axe to meet is
            // unwinnable. Only authored ropes matter: gun and auto-catch hooks carry the chain anchor
            // art without ever building a chain.
            if (axes.Count == 0
                && objects.Any(o => ChainRope.IsChain(o) && RopeResolver.Resolve(o, objects, document.TwoParts).Target is not null))
            {
                warnings.Add(new LevelWarning("Validation.ChainWithoutAxe"));
            }

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                string? candyNumber = grab.GetAttr("candyNumber");
                // An imported axed="true" grab keeps its axe key in candyNumber, so a key the axes do
                // answer to is not a dangling candy reference.
                if (candyNumber is not null
                    && !BindsToAnAxe(grab)
                    && !candyKeys.Any(k => string.Equals(k, candyNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add(new LevelWarning("Validation.GrabUnmatchedCandyNumber", candyNumber));
                }

                // An explicit axeNumber naming no axe silently falls back to the candy, which is never
                // what the author meant.
                if (grab.GetAttr(AxeBinding.KeyAttribute) is { } axeNumber && !BindsToAnAxe(grab))
                {
                    warnings.Add(new LevelWarning("Validation.GrabUnmatchedAxeNumber", axeNumber));
                }

                // A hook exactly above or below what its rope binds to starts the bungee as a perfectly
                // straight vertical line, and the game's solver has no basis for choosing a swing
                // direction, so it picks an arbitrary diagonal that fights gravity. A single pixel of
                // offset settles it - the shipped 1_1.xml puts the candy at 158 and the hook at 159.
                // Only authored ropes can hit this: RopeResolver already returns no target for gun and
                // auto-catch hooks, which take hold of the candy during play instead.
                RopeTarget rope = RopeResolver.Resolve(grab, objects, document.TwoParts);
                if (rope.Target is { } bound && grab.X == bound.X)
                {
                    warnings.Add(new LevelWarning("Validation.GrabVerticallyAligned", grab.X));
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

            // Rockets, water and snails were tuned against the mobile physics model: rockets and water
            // read their own ActivePhysicsConstants entries (RocketImpulseScale, WaterDamping,
            // WaterRocketImpulseDivisor, ...) whose values differ per model, and a snail applies a flat
            // weight to the candy point, so its pull depends on the model's gravity and scale. Under the
            // PC model they still load and play, they just feel off - advisory, not an error.
            if (!document.UseMobilePhysics)
            {
                if (HasType("rocket"))
                {
                    warnings.Add(new LevelWarning("Validation.RocketWithoutMobilePhysics"));
                }

                if (document.Water > 0f)
                {
                    warnings.Add(new LevelWarning("Validation.WaterWithoutMobilePhysics"));
                }

                // "load" is the snail's element name; see DescriptorTable.
                if (HasType("load"))
                {
                    warnings.Add(new LevelWarning("Validation.SnailWithoutMobilePhysics"));
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

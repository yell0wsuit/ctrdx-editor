using System;
using System.Collections.Generic;
using System.Globalization;
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

            // DX fits the declared map to whatever aspect ratio the window has, so only the declared
            // width x height is guaranteed on screen; anything past it is cropped off on non-16:9
            // displays. The game adds gameDesign mapOffsetX/Y after the x3 map scale while the camera
            // bounds ignore it (GameScene.Show / LoadMetadata), so it shifts objects by offset / 3
            // level units relative to the visible area.
            if (objects.Any(o => IsOutsideLevelBounds(o, document)))
            {
                warnings.Add(new LevelWarning("Validation.ObjectOutsideLevelBounds"));
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

            List<LevelObject> bombs = [.. objects.Where(BombBinding.IsBomb)];
            bool BindsToABomb(LevelObject grab)
            {
                return BombBinding.RequestedKey(grab) is { } key
                    && bombs.Any(b => AxeBinding.KeyEquals(BombBinding.KeyOf(b), key));
            }

            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                string? candyNumber = grab.GetAttr("candyNumber");
                // An imported axed="true" or bombed="true" grab keeps its key in candyNumber, so a key an
                // axe or bomb does answer to is not a dangling candy reference.
                if (candyNumber is not null
                    && !BindsToAnAxe(grab)
                    && !BindsToABomb(grab)
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

                // Same for a bombNumber, which binds nothing unless a bomb answers to it and the grab
                // also carries bombed="true".
                if (grab.GetAttr(BombBinding.KeyAttribute) is { } bombNumber && !BindsToABomb(grab))
                {
                    warnings.Add(new LevelWarning("Validation.GrabUnmatchedBombNumber", bombNumber));
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

                // Bombs come from Time Travel, a mobile-physics game, so their blast and trigger
                // distances are tuned against that model.
                if (HasType(BombBinding.Element))
                {
                    warnings.Add(new LevelWarning("Validation.BombWithoutMobilePhysics"));
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

            warnings.AddRange(TutorialValidation.Validate(document));

            // Errors cost the level content, so they lead; LINQ's sort is stable, which keeps each
            // group in the order the rules produced it.
            return [.. warnings.OrderBy(warning => warning.Severity == LevelWarningSeverity.Error ? 0 : 1)];
        }

        private const double MapScale = 3.0;

        private static bool IsOutsideLevelBounds(LevelObject obj, LevelDocument document)
        {
            // Elements without coordinates (if any) have no on-screen position to lose.
            if (obj.GetAttr("x") is null && obj.GetAttr("y") is null)
            {
                return false;
            }

            double x = obj.X + (ReadDesignInt(document, "mapOffsetX") / MapScale);
            double y = obj.Y + (ReadDesignInt(document, "mapOffsetY") / MapScale);
            return x < 0 || x > document.Width || y < 0 || y > document.Height;
        }

        private static int ReadDesignInt(LevelDocument document, string name)
        {
            return int.TryParse(
                document.GameDesignElement?.Attribute(name)?.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int v)
                ? v
                : 0;
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

using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>The resolved destination category for a grab rope.</summary>
    public enum RopeTargetKind
    {
        /// <summary>The rope targets a candy object.</summary>
        Candy,

        /// <summary>The rope targets a light bulb object.</summary>
        Bulb,

        /// <summary>The rope targets an axe object.</summary>
        Axe,

        /// <summary>The rope has no resolved target.</summary>
        None,
    }

    /// <summary>The resolved rope target kind and object, when one exists.</summary>
    public readonly record struct RopeTarget(RopeTargetKind Kind, LevelObject? Target);

    /// <summary>Resolves grab rope targets against the objects in a level, mirroring the game loaders.</summary>
    public static class RopeResolver
    {
        /// <summary>Finds the object a grab rope should visually connect to.</summary>
        public static RopeTarget Resolve(
            LevelObject grab, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            // The game builds the bungee only when radius == -1 && !gun (LoadGrabs). A gun grab, or an
            // auto-catch grab (positive radius), has no authored rope - it binds candy at runtime - so
            // the candy/bulb binding block is skipped and nothing is drawn.
            if (IsTrue(grab.GetAttr("gun")) || GrabRadius.Of(grab) is not null)
            {
                return new RopeTarget(RopeTargetKind.None, null);
            }

            bool bindBulb = IsTrue(grab.GetAttr("bindBulb"));
            if (bindBulb)
            {
                List<LevelObject> bulbs = [.. objects.Where(o => o.Type is "lightBulb" or "lightbulb")];
                if (bulbs.Count > 0)
                {
                    string? num = grab.GetAttr("bulbNumber");
                    // Exact match on the bulb key, else the last bulb present (game fallback).
                    LevelObject bulb = bulbs.LastOrDefault(o =>
                        KeyEquals(o.GetAttr("number") ?? o.GetAttr("bulbNumber"), num)) ?? bulbs[^1];
                    return new RopeTarget(RopeTargetKind.Bulb, bulb);
                }
                // No bulbs at all: fall through to the candy branch, as the game falls back to star.
                // Not to an axe - LoadGrabs only reaches its axe branch when bindBulb is off.
            }

            // LoadGrabs tries the axe before the candy, and an unmatched axe key drops through to the
            // candy branch rather than leaving the rope unbound.
            if (!bindBulb && AxeBinding.RequestedKey(grab) is { } axeKey)
            {
                LevelObject? axe = objects.FirstOrDefault(o =>
                    AxeBinding.IsAxe(o) && AxeBinding.KeyEquals(AxeBinding.KeyOf(o), axeKey));
                if (axe is not null)
                {
                    return new RopeTarget(RopeTargetKind.Axe, axe);
                }
            }

            LevelObject? candy;
            if (twoParts)
            {
                candy = objects.FirstOrDefault(o => o.Type == (grab.GetAttr("part") == "R" ? "candyR" : "candyL"));
            }
            else
            {
                List<LevelObject> candies = [.. objects.Where(o => o.Type == "candy")];
                string? key = grab.GetAttr("candyNumber");
                candy = key is not null
                    ? candies.FirstOrDefault(c => KeyEquals(c.GetAttr("candyNumber"), key)) ?? candies.FirstOrDefault()
                    : candies.FirstOrDefault();
            }

            return new RopeTarget(candy is null ? RopeTargetKind.None : RopeTargetKind.Candy, candy);
        }

        // Mirrors CandyMatch: both non-null, trimmed, case-insensitive.
        private static bool KeyEquals(string? a, string? b)
        {
            return a is not null && b is not null
                && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrue(string? v)
        {
            return bool.TryParse(v, out bool b) && b;
        }
    }
}

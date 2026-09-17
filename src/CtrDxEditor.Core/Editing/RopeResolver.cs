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

        /// <summary>The rope targets a bomb object.</summary>
        Bomb,

        /// <summary>The rope has no resolved target.</summary>
        None,
    }

    /// <summary>The resolved rope target kind and object, when one exists.</summary>
    public readonly record struct RopeTarget(RopeTargetKind Kind, LevelObject? Target);

    /// <summary>
    /// The objects a grab rope can bind to, gathered from a level once. Resolving many grabs against one
    /// <see cref="RopeCandidates"/> filters the level a single time rather than once per grab.
    /// </summary>
    public sealed class RopeCandidates
    {
        /// <summary>Gathers the bindable objects of a level, each list in level order.</summary>
        /// <param name="objects">All level objects.</param>
        public RopeCandidates(IReadOnlyList<LevelObject> objects)
        {
            foreach (LevelObject o in objects)
            {
                if (o.Type is "lightBulb" or "lightbulb")
                {
                    Bulbs.Add(o);
                }
                if (BombBinding.IsBomb(o))
                {
                    Bombs.Add(o);
                }
                if (AxeBinding.IsAxe(o))
                {
                    Axes.Add(o);
                }
                switch (o.Type)
                {
                    case "candy":
                        Candies.Add(o);
                        break;
                    case "candyL":
                        CandyL ??= o;
                        break;
                    case "candyR":
                        CandyR ??= o;
                        break;
                    default:
                        break;
                }
            }
        }

        internal List<LevelObject> Bulbs { get; } = [];

        internal List<LevelObject> Bombs { get; } = [];

        internal List<LevelObject> Axes { get; } = [];

        internal List<LevelObject> Candies { get; } = [];

        internal LevelObject? CandyL { get; }

        internal LevelObject? CandyR { get; }
    }

    /// <summary>Resolves grab rope targets against the objects in a level, mirroring the game loaders.</summary>
    public static class RopeResolver
    {
        /// <summary>Finds the object a grab rope should visually connect to.</summary>
        public static RopeTarget Resolve(
            LevelObject grab, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            return Resolve(grab, new RopeCandidates(objects), twoParts);
        }

        /// <summary>Finds the object a grab rope should visually connect to among already gathered candidates.</summary>
        public static RopeTarget Resolve(LevelObject grab, RopeCandidates candidates, bool twoParts)
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
                List<LevelObject> bulbs = candidates.Bulbs;
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

            // LoadGrabs tries a bombed grab's bomb first, then the axe, then the candy; an unmatched key
            // at either step drops through to the next rather than leaving the rope unbound.
            if (!bindBulb && BombBinding.RequestedKey(grab) is { } bombKey)
            {
                LevelObject? bomb = candidates.Bombs.FirstOrDefault(o =>
                    AxeBinding.KeyEquals(BombBinding.KeyOf(o), bombKey));
                if (bomb is not null)
                {
                    return new RopeTarget(RopeTargetKind.Bomb, bomb);
                }
            }

            if (!bindBulb && AxeBinding.RequestedKey(grab) is { } axeKey)
            {
                LevelObject? axe = candidates.Axes.FirstOrDefault(o =>
                    AxeBinding.KeyEquals(AxeBinding.KeyOf(o), axeKey));
                if (axe is not null)
                {
                    return new RopeTarget(RopeTargetKind.Axe, axe);
                }
            }

            LevelObject? candy;
            if (twoParts)
            {
                candy = grab.GetAttr("part") == "R" ? candidates.CandyR : candidates.CandyL;
            }
            else
            {
                List<LevelObject> candies = candidates.Candies;
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

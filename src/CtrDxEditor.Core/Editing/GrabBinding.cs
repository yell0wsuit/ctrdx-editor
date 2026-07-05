using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A single "Attach to" option: an opaque token plus its UI label.</summary>
    public readonly record struct GrabBindOption(string Token, string Label);

    /// <summary>
    /// Computes the grab "Attach to" choices, current selection, and apply-logic, faithful to the
    /// game's grab bind resolution. Tokens: "primary", "candy:{key}", "part:L", "part:R", "bulb:{key}".
    /// </summary>
    public static class GrabBinding
    {
        /// <summary>Builds the ordered options available for the level's mode and objects.</summary>
        public static IReadOnlyList<GrabBindOption> Options(IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            List<GrabBindOption> options = [];

            if (twoParts)
            {
                if (objects.Any(o => o.Type == "candyL"))
                {
                    options.Add(new GrabBindOption("part:L", "Candy (left)"));
                }
                if (objects.Any(o => o.Type == "candyR"))
                {
                    options.Add(new GrabBindOption("part:R", "Candy (right)"));
                }
            }
            else
            {
                List<LevelObject> candies = [.. objects.Where(o => o.Type == "candy")];
                for (int i = 0; i < candies.Count; i++)
                {
                    string key = candies[i].GetAttr("candyNumber") ?? "0";
                    options.Add(i == 0
                        ? new GrabBindOption("primary", $"Candy {key} (primary)")
                        : new GrabBindOption($"candy:{key}", $"Candy {key}"));
                }
            }

            foreach (LevelObject bulb in objects.Where(o => o.Type is "lightBulb" or "lightbulb"))
            {
                string key = bulb.GetAttr("bulbNumber") ?? "";
                options.Add(new GrabBindOption($"bulb:{key}", $"Bulb {key}"));
            }

            return options;
        }

        /// <summary>The token identifying a grab's current bind target.</summary>
        public static string CurrentToken(LevelObject grab, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            if (IsTrue(grab.GetAttr("bindBulb")))
            {
                return $"bulb:{grab.GetAttr("bulbNumber") ?? ""}";
            }

            if (twoParts)
            {
                return grab.GetAttr("part") == "R" ? "part:R" : "part:L";
            }

            string? key = grab.GetAttr("candyNumber");
            if (key is null)
            {
                return "primary";
            }

            List<LevelObject> candies = [.. objects.Where(o => o.Type == "candy")];
            // A key matching a non-primary candy selects it; anything else (unmatched, or the
            // primary's own key) resolves to the primary, mirroring the resolver fallback.
            for (int i = 1; i < candies.Count; i++)
            {
                if (KeyEquals(candies[i].GetAttr("candyNumber"), key))
                {
                    return $"candy:{key}";
                }
            }
            return "primary";
        }

        /// <summary>Applies a token to the grab, writing/clearing the underlying attributes.</summary>
        public static void Apply(LevelObject grab, string token)
        {
            if (token == "primary")
            {
                grab.RemoveAttr("candyNumber");
                grab.RemoveAttr("bindBulb");
                grab.RemoveAttr("bulbNumber");
            }
            else if (token.StartsWith("candy:", StringComparison.Ordinal))
            {
                grab.SetAttr("candyNumber", token["candy:".Length..]);
                grab.RemoveAttr("bindBulb");
                grab.RemoveAttr("bulbNumber");
            }
            else if (token is "part:L" or "part:R")
            {
                grab.SetAttr("part", token["part:".Length..]);
                grab.RemoveAttr("candyNumber");
                grab.RemoveAttr("bindBulb");
                grab.RemoveAttr("bulbNumber");
            }
            else if (token.StartsWith("bulb:", StringComparison.Ordinal))
            {
                grab.SetAttr("bindBulb", "true");
                grab.SetAttr("bulbNumber", token["bulb:".Length..]);
                grab.RemoveAttr("candyNumber");
            }
        }

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

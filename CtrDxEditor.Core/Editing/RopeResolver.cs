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

        /// <summary>The rope has no resolved target.</summary>
        None,
    }

    /// <summary>The resolved rope target kind and object, when one exists.</summary>
    public readonly record struct RopeTarget(RopeTargetKind Kind, LevelObject? Target);

    /// <summary>Resolves grab rope targets against the objects in a level.</summary>
    public static class RopeResolver
    {
        /// <summary>Finds the object a grab rope should visually connect to.</summary>
        public static RopeTarget Resolve(
            LevelObject grab, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            if (IsTrue(grab.GetAttr("gun")))
            {
                return new RopeTarget(RopeTargetKind.None, null);
            }

            if (IsTrue(grab.GetAttr("bindBulb")))
            {
                string? num = grab.GetAttr("bulbNumber");
                LevelObject? bulb = objects.FirstOrDefault(o =>
                    (o.Type is "lightBulb" or "lightbulb")
                    && (o.GetAttr("number") ?? o.GetAttr("bulbNumber")) == num);
                return new RopeTarget(bulb is null ? RopeTargetKind.None : RopeTargetKind.Bulb, bulb);
            }

            LevelObject? candy = twoParts
                ? objects.FirstOrDefault(o => o.Type == (grab.GetAttr("part") == "R" ? "candyR" : "candyL"))
                : objects.FirstOrDefault(o => o.Type == "candy");

            return new RopeTarget(candy is null ? RopeTargetKind.None : RopeTargetKind.Candy, candy);
        }

        private static bool IsTrue(string? v)
        {
            return bool.TryParse(v, out bool b) && b;
        }
    }
}

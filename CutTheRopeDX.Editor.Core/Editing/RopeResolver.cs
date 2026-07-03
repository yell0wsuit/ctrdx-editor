using System.Collections.Generic;
using System.Linq;

using CutTheRopeDX.Editor.Core.Document;

namespace CutTheRopeDX.Editor.Core.Editing
{
    public enum RopeTargetKind { Candy, Bulb, None }

    public readonly record struct RopeTarget(RopeTargetKind Kind, LevelObject? Target);

    public static class RopeResolver
    {
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

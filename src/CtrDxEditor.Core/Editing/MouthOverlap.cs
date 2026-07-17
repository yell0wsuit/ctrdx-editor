using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Finds candies whose authored start position already overlaps an Om Nom's mouth; such a candy
    /// is eaten the instant the mouth's idle cadence opens it, winning the level with no player input.
    /// Mirrors the game's eat test in GameScene.Update — GameObject.ObjectsIntersect(candy, target),
    /// an inclusive AABB overlap (CTRMathHelper.RectInRect) between the candy's bounding box and the
    /// target's mouth box. Both boxes come from HitboxTable, exactly where the game checks them.
    /// </summary>
    public static class MouthOverlap
    {
        private static readonly string[] CandyTypes = ["candy", "candyL", "candyR"];

        /// <summary>Candies whose collision box overlaps any Om Nom's mouth box.</summary>
        public static IReadOnlyList<LevelObject> CandiesOnMouth(LevelDocument document)
        {
            HitboxModel model = HitboxTable.ModelFor(document.UseMobilePhysics);
            IReadOnlyList<LevelObject> objects = document.Objects;
            List<LevelObject> result = [];
            foreach (LevelObject candy in objects)
            {
                if (!CandyTypes.Contains(candy.Type))
                {
                    continue;
                }
                if (CandyOnAnyMouth(candy, objects, model))
                {
                    result.Add(candy);
                }
            }
            return result;
        }

        /// <summary>True when the candy's box overlaps any target's mouth box.</summary>
        public static bool CandyOnAnyMouth(
            LevelObject candy, IReadOnlyList<LevelObject> objects, HitboxModel model)
        {
            if (HitboxTable.Compute(candy, scale: 1, model) is not { } candyBand)
            {
                return false;
            }
            foreach (LevelObject target in objects)
            {
                if (target.Type != "target")
                {
                    continue;
                }
                if (HitboxTable.Compute(target, scale: 1, model) is { } mouthBand
                    && Overlaps(candyBand, mouthBand))
                {
                    return true;
                }
            }
            return false;
        }

        // Inclusive AABB overlap, matching CTRMathHelper.RectInRect (edges touching counts as a hit).
        // Targets are never rotated, so no rotation handling is needed (unlike HazardOverlap).
        private static bool Overlaps(LevelBounds a, LevelBounds b)
        {
            return a.X <= b.X + b.W && a.X + a.W >= b.X
                && a.Y <= b.Y + b.H && a.Y + a.H >= b.Y;
        }
    }
}

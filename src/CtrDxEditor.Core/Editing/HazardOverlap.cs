using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Finds candies whose authored start position already sits inside a candy-breaking hazard
    /// (spikes / electro); such a candy dies the instant the level loads. Mirrors the game's
    /// BarrierCollision.Hits(point, band, spikeCollisionRadius: 15) — the candy center against the
    /// hazard's tolerance-inflated collision band, rotated by the hazard's display angle.
    /// </summary>
    public static class HazardOverlap
    {
        private static readonly string[] CandyTypes = ["candy", "candyL", "candyR"];

        /// <summary>Candies whose center lies inside any breaking hazard's collision region.</summary>
        public static IReadOnlyList<LevelObject> CandiesInHazards(LevelDocument document)
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
                if (CandyInAnyHazard(candy, objects, model))
                {
                    result.Add(candy);
                }
            }
            return result;
        }

        /// <summary>True when the candy center point is inside any breaking hazard's rotated band.</summary>
        public static bool CandyInAnyHazard(
            LevelObject candy, IReadOnlyList<LevelObject> objects, HitboxModel model)
        {
            foreach (LevelObject hazard in objects)
            {
                if (!IsBreakingHazard(hazard.Type))
                {
                    continue;
                }
                if (HitboxTable.Compute(hazard, scale: 1, model) is not { } band)
                {
                    continue;
                }
                double degrees = RotationTable.For(hazard.Type) is { } spec
                    ? ObjectRotation.DisplayDegrees(hazard, spec)
                    : 0;
                if (PointInRotatedBounds(candy.X, candy.Y, hazard.X, hazard.Y, degrees, band))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsBreakingHazard(string type)
        {
            return SpikeObject.IsSpike(type) || type == "electro";
        }

        // The drawn hitbox rotates the band by DisplayDegrees about the object center (see DrawHitbox);
        // the view transform is translation + uniform scale, so rotating the band in level space is
        // equivalent. Testing the point means rotating it by -degrees back into the band's local frame.
        private static bool PointInRotatedBounds(
            double px, double py, double cx, double cy, double degrees, LevelBounds band)
        {
            double rad = -degrees * Math.PI / 180.0;
            double dx = px - cx;
            double dy = py - cy;
            double rx = cx + (dx * Math.Cos(rad)) - (dy * Math.Sin(rad));
            double ry = cy + (dx * Math.Sin(rad)) + (dy * Math.Cos(rad));
            return rx >= band.X && rx < band.X + band.W
                && ry >= band.Y && ry < band.Y + band.H;
        }
    }
}

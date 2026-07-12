using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>One deterministic puff sampled from SteamTube's steady-state maximum plume.</summary>
    public readonly record struct SteamPuffSpec(
        int Quad,
        double LocalX,
        double LocalY,
        double Scale,
        bool Front);

    /// <summary>Exact SteamTube physical and input geometry ported from the game.</summary>
    public static class SteamTubeGeometry
    {
        /// <summary>ITransporterItem collision radius around the tube body, in raw game units.</summary>
        public const double BodyCollisionRadius = 52.5;

        /// <summary>Valve input radius, kept only to make clear that it is not the body indicator.</summary>
        public const double ValveTouchRadius = 40;

        /// <summary>Valve input center offset along local +Y, in raw game units.</summary>
        public const double ValveTouchOffset = 28;

        /// <summary>Tube atlas source height; anchor 10 places its top-center at the object origin.</summary>
        public const double BodySourceHeight = 253;

        /// <summary>Valve center offset after the game's heightScale/mapScale cancellation.</summary>
        public const double ValveDrawOffset = 27;

        /// <summary>Maximum-state puff endpoint after the game's heightScale/mapScale cancellation.</summary>
        public const double MaximumSteamHeight = 141;

        /// <summary>Center offset of the top-anchored tube art in level space.</summary>
        public static double BodyDrawCenterOffset(double mapScale = SpritePlacement.MapScale)
        {
            return BodySourceHeight / (2.0 * mapScale);
        }

        /// <summary>Returns the body's circular collision bounds in editor level space.</summary>
        public static LevelBounds BodyBounds(
            double x,
            double y,
            double mapScale = SpritePlacement.MapScale)
        {
            double radius = BodyCollisionRadius / mapScale;
            return new LevelBounds(x - radius, y - radius, radius * 2, radius * 2);
        }

        /// <summary>
        /// Samples the maximum-state 20-puff loop at evenly staggered mid-steps. Random height variance is
        /// fixed at its zero midpoint so the editor remains stable; all other frame, easing, scaling, side
        /// attenuation, and back/front rules match <c>SteamTube.AdjustSteam</c>.
        /// </summary>
        public static IReadOnlyList<SteamPuffSpec> MaximumPlume(
            double mapScale = SpritePlacement.MapScale)
        {
            const int puffCount = 20;
            SteamPuffSpec[] puffs = new SteamPuffSpec[puffCount];
            for (int i = 0; i < puffCount; i++)
            {
                int variant = i % 3;
                int firstQuad = variant switch
                {
                    0 => 24,
                    1 => 13,
                    _ => 2,
                };
                double progress = 1.0 - ((i + 0.5) / puffCount);
                int frameOffset = Math.Min(10, (int)(progress * 11));

                // Game endpoints are cast to int by KeyFrame.MakePos before interpolation.
                double endpointX = variant switch
                {
                    1 => 2.0 / mapScale,
                    2 => -2.0 / mapScale,
                    _ => 0,
                };
                double heightScale = variant == 0 ? 1.0 : 0.94;
                double endpointY = (int)(-MaximumSteamHeight * mapScale * heightScale) / mapScale;
                double eased = 1.0 - ((1.0 - progress) * (1.0 - progress));

                puffs[i] = new SteamPuffSpec(
                    Quad: firstQuad + frameOffset,
                    LocalX: endpointX * eased,
                    LocalY: endpointY * eased,
                    Scale: 1.0 + (0.5 * progress),
                    Front: variant != 0);
            }
            return puffs;
        }
    }
}

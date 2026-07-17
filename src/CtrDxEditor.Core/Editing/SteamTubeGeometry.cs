using System;
using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>One deterministic puff sampled from SteamTube's steady-state maximum plume.</summary>
    /// <param name="Quad">The puff's zero-based atlas quad.</param>
    /// <param name="LocalX">X offset from the tube, in level units.</param>
    /// <param name="LocalY">Y offset from the tube, in level units.</param>
    /// <param name="Scale">The per-puff sprite scale.</param>
    /// <param name="Front">Whether the puff draws in front of the tube rather than behind it.</param>
    public readonly record struct SteamPuffSpec(
        int Quad,
        double LocalX,
        double LocalY,
        double Scale,
        bool Front);

    /// <summary>Exact SteamTube physical and input geometry ported from the game.</summary>
    public static class SteamTubeGeometry
    {
        /// <summary>Trimmed tube quad height used by Image.SetDrawQuad and SteamTube.BindPoint.</summary>
        public const double BodyQuadHeight = 168;

        /// <summary>Valve center offset after the game's heightScale/mapScale cancellation.</summary>
        public const double ValveDrawOffset = 27;

        /// <summary>Maximum-state puff endpoint after the game's heightScale/mapScale cancellation.</summary>
        public const double MaximumSteamHeight = 141;

        /// <summary>Center offset of the top-anchored tube art in level space.</summary>
        public static double BodyDrawCenterOffset(double mapScale = SpritePlacement.MapScale)
        {
            return BodyQuadHeight / (2.0 * mapScale);
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

using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// One chain sprite: which atlas quad to draw, where to center it, how far to turn it, and the
    /// per-link tint. The sprite's size is not carried here - the renderer takes it from the atlas
    /// frame, exactly as the game reads <c>texture.quadRects</c>.
    /// </summary>
    /// <param name="QuadIndex">Atlas quad to draw: <see cref="ChainSpritePlanner.LinkQuad"/> or <see cref="ChainSpritePlanner.MidpointQuad"/>.</param>
    /// <param name="Center">Sprite center in level units.</param>
    /// <param name="Rotation">Sprite rotation in radians, clockwise in screen space.</param>
    /// <param name="Tint">Per-link color; white for half the links and a grey shade for the rest.</param>
    public readonly record struct ChainSprite(int QuadIndex, Vec2 Center, double Rotation, RopeRgba Tint);

    /// <summary>
    /// Lays out the sprites that draw a chain rope, porting the game's <c>Bungee.DrawChain</c> /
    /// <c>BuildChainSpritePlan</c>. A chain hangs on the same curve as an ordinary rope - it reuses
    /// <see cref="RopeStripBuilder.ControlPoints"/> - but is drawn as discrete links rather than a
    /// twisted cord: one link sprite at every bezier sample and one midpoint sprite between adjacent
    /// samples.
    /// </summary>
    /// <remarks>
    /// The game samples a chain far more coarsely than a cord (<c>ChainDrawSamplePoints</c> = 2 against
    /// the cord's 4), which is what gives the links their spacing; that constant is reproduced here.
    /// Christmas lights are deliberately absent: the game draws them inside <c>DrawBungee</c>, which a
    /// chain never reaches.
    /// </remarks>
    public static class ChainSpritePlanner
    {
        /// <summary>Atlas quad for the link drawn at each sampled curve point.</summary>
        public const int LinkQuad = 0;

        /// <summary>Atlas quad for the link drawn between two adjacent samples.</summary>
        public const int MidpointQuad = 1;

        // Bungee.ChainDrawSamplePoints: bezier samples per control-point segment.
        private const int SamplePoints = 2;

        /// <summary>
        /// Builds the chain sprites for a rope from <paramref name="a"/> (the grab) to
        /// <paramref name="b"/> (the target) with rest length <paramref name="length"/>.
        /// </summary>
        /// <param name="a">First endpoint (the grab), in level units.</param>
        /// <param name="b">Second endpoint (the target), in level units.</param>
        /// <param name="length">Rope rest length, in level units.</param>
        /// <param name="seed">Per-rope seed selecting which links are tinted; stable across redraws.</param>
        /// <param name="physics">The level's physics model; omitting it assumes the desktop model.</param>
        /// <returns>The chain sprites in draw order: every link, then every midpoint.</returns>
        public static IReadOnlyList<ChainSprite> Build(Vec2 a, Vec2 b, double length, int seed, RopePhysics? physics = null)
        {
            return Build(RopeStripBuilder.ControlPoints(a, b, length, physics), seed);
        }

        /// <summary>
        /// Builds the chain sprites along an already-computed curve.
        /// </summary>
        /// <param name="controls">The curve's control points, from grab to target.</param>
        /// <param name="seed">Per-rope seed selecting which links are tinted.</param>
        /// <returns>The chain sprites in draw order: every link, then every midpoint.</returns>
        public static IReadOnlyList<ChainSprite> Build(IReadOnlyList<Vec2> controls, int seed)
        {
            int count = controls?.Count ?? 0;
            int sampleCount = (count - 1) * SamplePoints;
            if (count < 2 || sampleCount <= 0)
            {
                return [];
            }

            // The game steps bezierT by 1/sampleCount from 0, so the last sample stops one step short
            // of the target rather than landing on it.
            Vec2[] samples = new Vec2[sampleCount];
            double sampleStep = 1.0 / sampleCount;
            double bezierT = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = RopeStripBuilder.CalcPathBezier(controls!, bezierT);
                bezierT += sampleStep;
            }

            List<ChainSprite> sprites = new(sampleCount + Math.Max(0, sampleCount - 1));
            for (int i = 0; i < sampleCount; i++)
            {
                double angle = i == 0 ? 0 : Angle(samples[i - 1], samples[i]);
                sprites.Add(new ChainSprite(LinkQuad, samples[i], angle, Tint(seed, sprites.Count)));
            }
            for (int i = 0; i < sampleCount - 1; i++)
            {
                Vec2 center = new(
                    samples[i].X + ((samples[i + 1].X - samples[i].X) * 0.5),
                    samples[i].Y + ((samples[i + 1].Y - samples[i].Y) * 0.5));
                sprites.Add(new ChainSprite(MidpointQuad, center, Angle(samples[i], samples[i + 1]), Tint(seed, sprites.Count)));
            }

            return sprites;
        }

        // Bungee.GetChainAngle.
        private static double Angle(Vec2 previous, Vec2 current)
        {
            return Math.Atan2(previous.Y - current.Y, previous.X - current.X) + (Math.PI / 2);
        }

        /// <summary>
        /// The per-link color, porting <c>Bungee.BuildChainSpriteColors</c>: a stable hash of the seed
        /// and link index leaves half the links opaque white and shades the rest, so a chain reads as
        /// individually lit links instead of a flat ribbon. Alpha is always 1 - the game varies it only
        /// while a cut chain fades out, which the editor has no state for.
        /// </summary>
        /// <param name="seed">Per-rope seed.</param>
        /// <param name="index">Link index within the rope.</param>
        /// <returns>The link's tint.</returns>
        private static RopeRgba Tint(int seed, int index)
        {
            uint hash = Hash(seed, index);
            if ((hash & 1) != 0)
            {
                return new RopeRgba(1, 1, 1, 1);
            }

            // Bungee.GetChainMaskColor: a grey in [0.5, 1].
            double grey = 0.5 + (((hash >> 8) & 0xFF) / 255.0 * 0.5);
            return new RopeRgba(grey, grey, grey, 1);
        }

        // Bungee.HashChainSprite. Deliberately unchecked: the game relies on the wrap-around.
        private static uint Hash(int seed, int index)
        {
            unchecked
            {
                uint h = (uint)(seed * 73856093) ^ (uint)((index + 1) * 19349663);
                h ^= h >> 13;
                h *= 0x5BD1E995u;
                h ^= h >> 15;
                return h;
            }
        }
    }
}

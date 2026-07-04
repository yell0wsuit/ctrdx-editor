using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the game-accurate rope strip builder.</summary>
    public class RopeStripBuilderTests
    {
        /// <summary>With two controls the bezier is a straight lerp.</summary>
        [Fact]
        public void CalcPathBezierTwoControlsLerps()
        {
            List<Vec2> controls = [new Vec2(0, 0), new Vec2(10, 20)];
            Vec2 mid = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            Assert.Equal(5, mid.X, 9);
            Assert.Equal(10, mid.Y, 9);
        }

        /// <summary>The curve interpolates the first and last control points exactly.</summary>
        [Fact]
        public void CalcPathBezierHitsEndpoints()
        {
            List<Vec2> controls = [new Vec2(1, 2), new Vec2(50, 90), new Vec2(-4, 30), new Vec2(7, 8)];
            Vec2 start = RopeStripBuilder.CalcPathBezier(controls, 0);
            Vec2 end = RopeStripBuilder.CalcPathBezier(controls, 1);
            Assert.Equal(new Vec2(1, 2), start);
            Assert.Equal(new Vec2(7, 8), end);
        }

        /// <summary>Symmetric controls give a symmetric curve: the midpoint sits on the axis of symmetry.</summary>
        [Fact]
        public void CalcPathBezierSymmetricControlsMidpointCentered()
        {
            List<Vec2> controls = [new Vec2(0, 0), new Vec2(50, 100), new Vec2(100, 0)];
            Vec2 mid = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            Assert.Equal(50, mid.X, 9);
            Assert.Equal(50, mid.Y, 9); // quadratic: 0.25*0 + 0.5*100 + 0.25*0
        }

        /// <summary>A taut rope (length &lt; distance) is straight: strip centerlines stay on the chord.</summary>
        [Fact]
        public void BuildTautRopeIsStraight()
        {
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290).Strips;
            Assert.NotEmpty(strips);
            foreach (RopeStrip s in strips)
            {
                Assert.Equal(0, s.Points[4].Y, 6); // p1 (centerline)
                Assert.Equal(0, s.Points[5].Y, 6); // p2 (centerline)
            }
        }

        /// <summary>Each strip is the game's 10-vertex cross-section: transparent edges, opaque center.</summary>
        [Fact]
        public void BuildStripStructureMatchesGame()
        {
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290).Strips;
            foreach (RopeStrip s in strips)
            {
                Assert.Equal(10, s.Points.Length);
                Assert.Equal(10, s.Colors.Length);
                Assert.Equal(0, s.Colors[0].A); // AA fade edges
                Assert.Equal(0, s.Colors[1].A);
                Assert.Equal(0, s.Colors[8].A);
                Assert.Equal(0, s.Colors[9].A);
                Assert.Equal(1, s.Colors[4].A); // centerline opaque
                Assert.Equal(1, s.Colors[5].A);
                // Edge band is the center color brightened by the game's +0.15.
                Assert.Equal(s.Colors[4].R + 0.15, s.Colors[2].R, 9);
            }
        }

        /// <summary>A slack rope sags: some centerline point drops below both endpoints (+Y is down).</summary>
        [Fact]
        public void BuildSlackRopeSags()
        {
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 400).Strips;
            bool sagged = false;
            foreach (RopeStrip s in strips)
            {
                if (s.Points[4].Y > 10)
                {
                    sagged = true;
                }
            }

            Assert.True(sagged);
        }

        /// <summary>Color batches alternate between the two rope tracks (the twisted-cord look).</summary>
        [Fact]
        public void BuildAlternatesColorTracks()
        {
            // Length 300 at rest length 35 -> 10 constraint points -> 36 samples; batches of 3 segments.
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(200, 0), 300).Strips;
            Assert.True(strips.Count >= 4);
            RopeRgba first = strips[0].Colors[4];  // batch 1: track 2 (Shade2 ramp)
            RopeRgba fourth = strips[3].Colors[4]; // batch 2: track 1 (Shade1 ramp)
            Assert.NotEqual(first, fourth);
        }

        /// <summary>The along-rope ramp brightens: a late strip is brighter than the first (shade -> base).</summary>
        [Fact]
        public void BuildRampsShadeToBase()
        {
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(200, 0), 300).Strips;
            RopeRgba early = strips[0].Colors[4];
            RopeRgba late = strips[^1].Colors[4];
            Assert.True(late.R > early.R);
        }

        /// <summary>Overstretched ropes get the game's red shade boost (raw channel exceeds 1 before clamping).</summary>
        [Fact]
        public void BuildOverstretchedTintsRed()
        {
            // Distance 300 vs length 100: far past the 7/105 threshold; shade red *= (300/100)*2.
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 100).Strips;
            Assert.True(strips[0].Colors[4].R > 1);
        }

        /// <summary>A taut rope below the stretch threshold keeps the resting palette.</summary>
        [Fact]
        public void BuildTautButNotStretchedKeepsRestingColors()
        {
            // 300 apart, rope 290: taut, but only ~3.4% over rest (threshold is ~6.67%).
            IReadOnlyList<RopeStrip> taut = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290).Strips;
            IReadOnlyList<RopeStrip> slack = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 400).Strips;
            Assert.Equal(slack[0].Colors[4], taut[0].Colors[4]);
        }

        /// <summary>Coincident endpoints produce no strips (all segments degenerate).</summary>
        [Fact]
        public void BuildCoincidentEndpointsReturnsEmpty()
        {
            IReadOnlyList<RopeStrip> strips = RopeStripBuilder.Build(new Vec2(50, 50), new Vec2(50, 50), 0).Strips;
            Assert.Empty(strips);
        }

        /// <summary>Build exposes the bezier polyline (the game's drawPts): one point per sample, ends pinned.</summary>
        [Fact]
        public void BuildExposesSamplePoints()
        {
            // Length 290 -> 10 constraint points -> 36 samples -> 37 polyline points (t = 0..1 inclusive).
            RopeVisual visual = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290);
            Assert.Equal(37, visual.SamplePoints.Count);
            Assert.Equal(new Vec2(0, 0), visual.SamplePoints[0]);
            Assert.Equal(300, visual.SamplePoints[^1].X, 6);
        }

        /// <summary>Lights sit on every 6th sample point, skipping 4 points at each end (game cadence).</summary>
        [Fact]
        public void ChristmasLightPointsFollowGameCadence()
        {
            List<Vec2> samples = [];
            for (int i = 0; i < 37; i++)
            {
                samples.Add(new Vec2(i, 0));
            }

            List<Vec2> lights = RopeStripBuilder.ChristmasLightPoints(samples);
            Assert.Equal([4.0, 10.0, 16.0, 22.0, 28.0], lights.ConvertAll(p => p.X));
        }

        /// <summary>Ropes too short for the end skips get no lights.</summary>
        [Fact]
        public void ChristmasLightPointsShortRopeHasNone()
        {
            List<Vec2> samples = [];
            for (int i = 0; i < 8; i++)
            {
                samples.Add(new Vec2(i, 0));
            }

            Assert.Empty(RopeStripBuilder.ChristmasLightPoints(samples));
        }
    }
}

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

        /// <summary>
        /// The control chain matches what Bungee.RollplacingWithOffset builds: the anchor and tail, plus
        /// one part per rest length or part thereof. A rest length is 35 in level units (the game's 105
        /// world units over its map scale of 3), so a partial remainder still costs a whole part.
        /// </summary>
        [Theory]
        [InlineData(0, 2)]     // nothing to subdivide: just the anchor and the tail
        [InlineData(1, 3)]     // any length at all rolls up to one part
        [InlineData(35, 3)]    // exactly one rest length
        [InlineData(36, 4)]    // one over: the remainder still costs a part
        [InlineData(100, 5)]   // the default authored rope length
        [InlineData(210, 8)]
        [InlineData(290, 11)]
        public void ControlPointCountFollowsTheRestLengthChain(double length, int expected)
        {
            Assert.Equal(expected, RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), length).Length);
        }

        /// <summary>
        /// The mobile model subdivides on its own rest length. The game reads BungeeRestLength through
        /// ActivePhysicsConstants, which is 105 world units on desktop but 30 raw scaled by
        /// Wp7ToWorldScale to 90 on mobile - 35 and 30 in level units - so the same rope holds more
        /// parts under mobile physics.
        /// </summary>
        [Theory]
        [InlineData(0, 2, 2)]
        [InlineData(30, 3, 3)]     // exactly one mobile rest length
        [InlineData(31, 3, 4)]     // over mobile's, still inside desktop's
        [InlineData(100, 5, 6)]
        [InlineData(210, 8, 9)]
        public void TheMobileModelSubdividesOnItsOwnRestLength(double length, int desktop, int mobile)
        {
            Assert.Equal(desktop,
                RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), length, RopePhysics.Desktop).Length);
            Assert.Equal(mobile,
                RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), length, RopePhysics.Mobile).Length);
        }

        /// <summary>
        /// The mobile model also samples each segment less densely - BungeeDrawSamplePoints is 3 against
        /// the desktop's 4 - so the same rope is drawn from fewer points.
        /// </summary>
        [Fact]
        public void TheMobileModelSamplesEachSegmentLessDensely()
        {
            RopeVisual desktop = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290, physics: RopePhysics.Desktop);
            RopeVisual mobile = RopeStripBuilder.Build(new Vec2(0, 0), new Vec2(300, 0), 290, physics: RopePhysics.Mobile);

            Assert.Equal(4, RopePhysics.Desktop.SamplesPerSegment);
            Assert.Equal(3, RopePhysics.Mobile.SamplesPerSegment);

            // 11 controls over 4 samples each is 40 steps, and 12 over 3 each is 33. The sampler walks
            // t from 0 to 1 inclusive, so a clean step lands exactly on 1 and yields steps + 1 points.
            // A step of 1/33 does not: accumulating it 33 times falls a hair short, costing one extra
            // pass before the clamp. The game's own loop accumulates bezierT the same way.
            Assert.Equal(41, desktop.SamplePoints.Count);
            Assert.Equal(35, mobile.SamplePoints.Count);
        }

        /// <summary>
        /// A level that has not opted into mobile physics uses the desktop model, which is what the
        /// geometry helpers assume when no model is named.
        /// </summary>
        [Fact]
        public void TheModelFollowsTheLevelSetting()
        {
            Assert.Equal(RopePhysics.Desktop, RopePhysics.For(useMobilePhysics: false));
            Assert.Equal(RopePhysics.Mobile, RopePhysics.For(useMobilePhysics: true));
            Assert.Equal(
                RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), 100, RopePhysics.Desktop).Length,
                RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), 100).Length);
        }

        /// <summary>A taut rope's controls lie on the straight chord.</summary>
        [Fact]
        public void TautControlPointsLieOnTheChord()
        {
            Vec2[] pts = RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), length: 80);

            Assert.Equal(new Vec2(0, 0), pts[0]);
            Assert.Equal(new Vec2(100, 0), pts[^1]);
            foreach (Vec2 p in pts)
            {
                Assert.Equal(0, p.Y, 6);
            }
        }

        /// <summary>A slack rope's interior controls hang below the chord (+Y is down).</summary>
        [Fact]
        public void SlackControlPointsSagBelowTheChord()
        {
            Vec2[] pts = RopeStripBuilder.ControlPoints(new Vec2(0, 0), new Vec2(100, 0), length: 200);

            Assert.Equal(new Vec2(0, 0), pts[0]);
            Assert.Equal(new Vec2(100, 0), pts[^1]);
            Assert.True(pts[pts.Length / 2].Y > 0);
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
            Assert.Equal(41, visual.SamplePoints.Count);
            Assert.Equal(new Vec2(0, 0), visual.SamplePoints[0]);
            Assert.Equal(300, visual.SamplePoints[^1].X, 6);
        }

        /// <summary>A non-default skin produces different rope colors than the default skin.</summary>
        [Fact]
        public void BuildHonorsSkinProducesDifferentColorsThanDefault()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(50, 0);
            RopeVisual def = RopeStripBuilder.Build(a, b, 60, skin: 0);
            RopeVisual blue = RopeStripBuilder.Build(a, b, 60, skin: 2);
            RopeRgba defColor = def.Strips[0].Colors[0];
            RopeRgba blueColor = blue.Strips[0].Colors[0];
            Assert.NotEqual((defColor.R, defColor.G, defColor.B), (blueColor.R, blueColor.G, blueColor.B));
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

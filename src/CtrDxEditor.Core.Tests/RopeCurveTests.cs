using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the static rope sag-curve approximation.</summary>
    public class RopeCurveTests
    {
        private static double PolylineLength(IReadOnlyList<Vec2> pts)
        {
            double sum = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                double dx = pts[i].X - pts[i - 1].X;
                double dy = pts[i].Y - pts[i - 1].Y;
                sum += Math.Sqrt((dx * dx) + (dy * dy));
            }
            return sum;
        }

        private static int LowestIndex(IReadOnlyList<Vec2> pts)
        {
            int idx = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                if (pts[i].Y > pts[idx].Y)
                {
                    idx = i;
                }
            }
            return idx;
        }

        /// <summary>A rope no longer than the gap is drawn straight (endpoints only).</summary>
        [Fact]
        public void TautRopeIsStraight()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(100, 0);

            IReadOnlyList<Vec2> pts = RopeCurve.Sample(a, b, length: 80);

            Assert.Equal(2, pts.Count);
            Assert.Equal(a, pts[0]);
            Assert.Equal(b, pts[^1]);
        }

        /// <summary>A slack rope sags: the midpoint sits below the chord midpoint (greater Y = down).</summary>
        [Fact]
        public void SlackRopeSagsBelowChord()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(100, 0);

            IReadOnlyList<Vec2> pts = RopeCurve.Sample(a, b, length: 160, segments: 20);

            Assert.Equal(21, pts.Count);
            Assert.Equal(a, pts[0]);
            Assert.Equal(b, pts[^1]);
            Assert.True(pts[10].Y > 0, "curve midpoint should droop downward");
        }

        /// <summary>More slack produces a deeper sag.</summary>
        [Fact]
        public void MoreSlackSagsDeeper()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(100, 0);

            double shallow = RopeCurve.Sample(a, b, length: 130, segments: 20)[10].Y;
            double deep = RopeCurve.Sample(a, b, length: 200, segments: 20)[10].Y;

            Assert.True(deep > shallow, "longer rope should sag deeper");
        }

        /// <summary>On a tilted chord the lowest point sits nearer the lower endpoint (catenary asymmetry).</summary>
        [Fact]
        public void TiltedChordLowPointNearerLowerEnd()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(100, 40); // +Y is down, so b is the lower endpoint

            IReadOnlyList<Vec2> pts = RopeCurve.Sample(a, b, length: 200, segments: 20);

            double lowX = pts[LowestIndex(pts)].X;
            Assert.True(lowX > 50, "catenary low point should shift toward the lower (right) endpoint");
        }

        /// <summary>The sampled polyline length approximates the requested rope length (validates the a-solve).</summary>
        [Fact]
        public void SampledLengthApproximatesRopeLength()
        {
            Vec2 a = new(0, 0);
            Vec2 b = new(100, 0);

            IReadOnlyList<Vec2> pts = RopeCurve.Sample(a, b, length: 160, segments: 64);

            Assert.Equal(160, PolylineLength(pts), tolerance: 2.0);
        }

        /// <summary>Coincident/near-vertical endpoints do not throw, produce NaN, or drop the endpoints.</summary>
        [Fact]
        public void CoincidentEndpointsAreSafe()
        {
            Vec2 a = new(50, 50);

            IReadOnlyList<Vec2> pts = RopeCurve.Sample(a, a, length: 100, segments: 20);

            Assert.Equal(21, pts.Count);
            Assert.Equal(a, pts[0]);
            Assert.Equal(a, pts[^1]);
            Assert.All(pts, p => Assert.False(double.IsNaN(p.X) || double.IsNaN(p.Y)));
            Assert.True(pts[10].Y > a.Y, "a slack rope between coincident points should hang below them");
        }
    }
}

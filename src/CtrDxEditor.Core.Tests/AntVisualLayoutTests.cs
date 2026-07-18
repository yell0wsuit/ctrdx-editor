using System.Linq;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for deterministic ant-conveyor visual layout math.</summary>
    public class AntVisualLayoutTests
    {
        /// <summary>Open paths use game spacing and show entrance and exit holes.</summary>
        [Fact]
        public void OpenLayoutUsesGameSpacingAndEndpointHoles()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: null);

            Assert.False(layout.Closed);
            Assert.Equal(2, layout.Holes.Count);
            Assert.Equal([0d, 35d, 70d], layout.Ants.Select(a => a.PathOffset));
        }

        /// <summary>Explicit loops omit holes and wrap live offsets around total path length.</summary>
        [Fact]
        public void ClosedLayoutWrapsAndOmitsHoles()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "100,0,0,0", 50, elapsedSeconds: 2);

            Assert.True(layout.Closed);
            Assert.Empty(layout.Holes);
            Assert.Equal([100d, 135d, 170d, 5d, 40d], layout.Ants.Select(a => a.PathOffset));
        }

        /// <summary>Negative speed advances ants backward and respawns them at the opposite open endpoint.</summary>
        [Fact]
        public void NegativeSpeedReversesOpenPreview()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "105,0", -10, elapsedSeconds: 1);

            Assert.Equal([95d, 25d, 60d], layout.Ants.Select(a => a.PathOffset));
            Assert.Equal(new Vec2(95, 0), layout.Ants[0].Position);
        }

        /// <summary>Degenerate segments are skipped without losing interpolation along valid segments.</summary>
        [Fact]
        public void ZeroLengthSegmentsAreSkippedDuringInterpolation()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(10, 20), "0,0,100,0", 0, elapsedSeconds: null);

            Assert.Equal(new Vec2(45, 20), layout.Ants[1].Position);
            Assert.All(layout.Ants, ant => Assert.Equal(0d, ant.HeadingDeg, 6));
        }

        /// <summary>Heading begins blending toward the next segment within fifteen units of a corner.</summary>
        [Fact]
        public void HeadingBlendsNearCorners()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "100,0,100,100", 95, elapsedSeconds: 1);

            Assert.Equal(new Vec2(95, 0), layout.Ants[0].Position);
            Assert.Equal(30d, layout.Ants[0].HeadingDeg, 6);
        }

        /// <summary>Open endpoints fade and scale while interior ants retain their deterministic base variants.</summary>
        [Fact]
        public void OpenEndpointsFadeAndStaticVariantsAreDeterministic()
        {
            AntVisualLayout first = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: null);
            AntVisualLayout second = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: null);

            Assert.Equal(first.Ants, second.Ants);
            Assert.Equal(first.Holes, second.Holes);
            Assert.Equal(first.Bounds, second.Bounds);
            Assert.Equal(first.Closed, second.Closed);
            Assert.Equal(0d, first.Ants[0].Opacity);
            Assert.Equal(0.2 * first.Ants[0].BaseScale, first.Ants[0].Scale, 6);
            Assert.Equal(1d, first.Ants[1].Opacity);
            Assert.Equal([0, 1, 2], first.Ants.Select(a => a.Frame));
        }

        /// <summary>Layout bounds cover the path and the fixed sixteen-unit artwork padding.</summary>
        [Fact]
        public void BoundsCoverAllVerticesAndArtworkPadding()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(20, 30), "100,0,100,80", 100, elapsedSeconds: null);

            Assert.Equal(new LevelBounds(4, 14, 132, 112), layout.Bounds);
        }
    }
}

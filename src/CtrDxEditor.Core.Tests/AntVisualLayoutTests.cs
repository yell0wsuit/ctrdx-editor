using System;
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
            Assert.Equal(new Vec2(-15.5, 0), layout.Holes[0].Position);
            Assert.Equal(new Vec2(105, 0), layout.Holes[1].Position);
            Assert.Equal([-15.5, 19.5, 54.5], layout.Ants.Select(a => a.PathOffset));
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

        /// <summary>Negative speed follows the game's backward extrapolation instead of wrapping to the exit.</summary>
        [Fact]
        public void NegativeSpeedReversesOpenPreview()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "105,0", -10, elapsedSeconds: 1);

            Assert.Equal([-25.5, 9.5, 44.5], layout.Ants.Select(a => a.PathOffset));
            Assert.Equal(new Vec2(-25.5, 0), layout.Ants[0].Position);
        }

        /// <summary>Forward preview removes ants at the exit and respawns them behind the entrance hole.</summary>
        [Fact]
        public void OpenPreviewUsesGameSpawnAndDespawnLifecycle()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: 0.35);

            Assert.Equal([19.5, 54.5, 89.5, -15.5], layout.Ants.Select(a => a.PathOffset));
        }

        /// <summary>Respawned ants retain deterministic variants when an older ant leaves the path.</summary>
        [Fact]
        public void RespawnedAntVariantsRemainStableForTheirLifetime()
        {
            AntVisualLayout beforeExit = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: 1.4);
            AntVisualLayout afterExit = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 100, elapsedSeconds: 1.6);
            AntVisual before = Assert.Single(beforeExit.Ants, ant => Math.Abs(ant.PathOffset - 54.5) < 0.000001);
            AntVisual after = Assert.Single(afterExit.Ants, ant => Math.Abs(ant.PathOffset - 74.5) < 0.000001);
            int beforePhase = (int)(Math.Floor(1.4 / 0.05) % 6);
            int afterPhase = (int)(Math.Floor(1.6 / 0.05) % 6);

            Assert.Equal(before.BaseScale, after.BaseScale);
            Assert.Equal((before.Frame - beforePhase + 6) % 6, (after.Frame - afterPhase + 6) % 6);
        }

        /// <summary>Degenerate segments are skipped without losing interpolation along valid segments.</summary>
        [Fact]
        public void ZeroLengthSegmentsAreSkippedDuringInterpolation()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(10, 20), "0,0,100,0", 0, elapsedSeconds: null);

            Assert.Equal(new Vec2(29.5, 20), layout.Ants[1].Position);
            Assert.All(layout.Ants, ant => Assert.Equal(0d, ant.HeadingDeg, 6));
        }

        /// <summary>Heading begins blending toward the next segment within fifteen units of a corner.</summary>
        [Fact]
        public void HeadingBlendsNearCorners()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(0, 0), "100,0,100,100", 110.5, elapsedSeconds: 1);

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

        /// <summary>Live preview advances the six walk frames at the game's fifty-millisecond cadence.</summary>
        [Fact]
        public void LivePreviewAdvancesWalkFrames()
        {
            AntVisualLayout initial = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 0, elapsedSeconds: 0);
            AntVisualLayout advanced = AntVisualLayout.Build(new Vec2(0, 0), "105,0", 0, elapsedSeconds: 0.05);

            Assert.Equal([0, 1, 2], initial.Ants.Select(a => a.Frame));
            Assert.Equal([1, 2, 3], advanced.Ants.Select(a => a.Frame));
        }

        /// <summary>Layout bounds cover the path, ant art, and the entrance hole behind the first vertex.</summary>
        [Fact]
        public void BoundsCoverAllVerticesAndArtworkPadding()
        {
            AntVisualLayout layout = AntVisualLayout.Build(new Vec2(20, 30), "100,0,100,80", 100, elapsedSeconds: null);

            Assert.Equal(new LevelBounds(-16, -6, 172, 152), layout.Bounds);
        }
    }
}

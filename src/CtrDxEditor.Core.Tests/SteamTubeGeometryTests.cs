using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using System.Linq;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests physical SteamTube geometry that is distinct from its valve touch target.</summary>
    public class SteamTubeGeometryTests
    {
        /// <summary>The body uses the game's transporter collision radius, not the valve touch zone.</summary>
        [Fact]
        public void BodyCollisionUsesExactGameRadius()
        {
            Assert.Equal(52.5, SteamTubeGeometry.BodyCollisionRadius);
            Assert.NotEqual(SteamTubeGeometry.ValveTouchRadius, SteamTubeGeometry.BodyCollisionRadius);
            Assert.Equal(40, SteamTubeGeometry.ValveTouchRadius);
            Assert.Equal(28, SteamTubeGeometry.ValveTouchOffset);
        }

        /// <summary>Raw game-space radius maps through the standard world-to-level scale.</summary>
        [Fact]
        public void BodyBoundsConvertToLevelSpaceAroundPipeOrigin()
        {
            LevelBounds bounds = SteamTubeGeometry.BodyBounds(100, 200);

            Assert.Equal(82.5, bounds.X);
            Assert.Equal(182.5, bounds.Y);
            Assert.Equal(35, bounds.W);
            Assert.Equal(35, bounds.H);
        }

        /// <summary>Maximum steam freezes the game's 20 staggered puffs across its three frame loops.</summary>
        [Fact]
        public void MaximumPlumeMatchesGamePuffCountVariantsAndLayers()
        {
            SteamPuffSpec[] puffs = [.. SteamTubeGeometry.MaximumPlume()];

            Assert.Equal(20, puffs.Length);
            Assert.Equal(7, puffs.Count(p => !p.Front));
            Assert.Equal(13, puffs.Count(p => p.Front));
            Assert.All(puffs, p => Assert.InRange(p.Quad, 2, 34));
            Assert.All(puffs.Where((_, i) => i % 3 == 0), p => Assert.InRange(p.Quad, 24, 34));
            Assert.All(puffs.Where((_, i) => i % 3 == 1), p => Assert.InRange(p.Quad, 13, 23));
            Assert.All(puffs.Where((_, i) => i % 3 == 2), p => Assert.InRange(p.Quad, 2, 12));
        }

        /// <summary>The frozen cycle fills the whole plume using game easing, side attenuation, and scale.</summary>
        [Fact]
        public void MaximumPlumeUsesSteadyStateTimelinePhases()
        {
            SteamPuffSpec[] puffs = [.. SteamTubeGeometry.MaximumPlume()];

            Assert.Equal(0, puffs[0].LocalX, 6);
            Assert.True(puffs[0].LocalY < -140);
            Assert.Equal(1.4875, puffs[0].Scale, 6);

            Assert.True(puffs[^1].LocalY < 0);
            Assert.True(puffs[^1].LocalY > -10);
            Assert.Equal(1.0125, puffs[^1].Scale, 6);
        }
    }
}

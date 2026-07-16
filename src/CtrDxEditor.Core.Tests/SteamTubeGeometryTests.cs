using System.Linq;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the frozen SteamTube plume geometry ported from the game.</summary>
    public class SteamTubeGeometryTests
    {
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

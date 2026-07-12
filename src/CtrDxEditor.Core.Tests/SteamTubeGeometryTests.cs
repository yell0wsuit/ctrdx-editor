using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

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
    }
}

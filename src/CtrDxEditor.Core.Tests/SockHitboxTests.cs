using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the magic hat's ported "mouth" collision box.</summary>
    public class SockHitboxTests
    {
        /// <summary>The mouth box at the origin is the game constants divided by MapScale (3).</summary>
        [Fact]
        public void MouthBoxAtOriginUsesGameConstantsOverMapScale()
        {
            LevelBounds box = SockHitbox.Compute(0, 0);

            Assert.Equal(-30.0, box.X, precision: 9);       // (x - 70 - 20) world = -90; -90/3
            Assert.Equal(0.0, box.Y, precision: 9);         // top edge at hat center
            Assert.Equal(140.0 / 3.0, box.W, precision: 9); // 140 world / 3
            Assert.Equal(15.0 / 3.0, box.H, precision: 9);  // 15 world / 3
        }

        /// <summary>The mouth box translates with the hat position.</summary>
        [Fact]
        public void MouthBoxTranslatesWithObjectPosition()
        {
            LevelBounds box = SockHitbox.Compute(100, 200);

            Assert.Equal(70.0, box.X, precision: 9);        // 100 - 30
            Assert.Equal(200.0, box.Y, precision: 9);
            Assert.Equal(140.0 / 3.0, box.W, precision: 9);
            Assert.Equal(15.0 / 3.0, box.H, precision: 9);
        }
    }
}

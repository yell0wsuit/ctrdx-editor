using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the magic hat's ported vertical sprite placement.</summary>
    public class SockSpriteTests
    {
        /// <summary>
        /// The game anchors the hat 85 world-units below the untrimmed sprite top; the editor
        /// center-anchors it (sourceHeight/2 below the top). The downward offset is the difference,
        /// scaled to level units: (431/2 - 85) * 0.7 / 3 = 30.45.
        /// </summary>
        [Fact]
        public void HatDrawOffsetMovesCenterAnchoredSpriteOntoGameAnchor()
        {
            double offset = SockSprite.DrawOffsetY(sourceHeight: 431, scale: 0.7);

            Assert.Equal(30.45, offset, precision: 9);
        }

        /// <summary>The offset scales with the sprite scale and shrinks with a larger map scale.</summary>
        [Fact]
        public void DrawOffsetScalesWithSpriteScaleAndMapScale()
        {
            Assert.Equal(((431 / 2.0) - 85) * 1.0 / 3.0, SockSprite.DrawOffsetY(431, 1.0), precision: 9);
            Assert.Equal(((431 / 2.0) - 85) * 0.7 / 6.0, SockSprite.DrawOffsetY(431, 0.7, 6.0), precision: 9);
        }
    }
}

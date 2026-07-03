using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for mapping game hitboxes into editor level space.</summary>
    public class HitboxTests
    {
        // scale = 3 makes s = scale/mapScale = 1, so level units == game px for clean expectations.

        /// <summary>Verifies that the candy desktop hitbox is centered using its source-size frame.</summary>
        [Fact]
        public void CandyDesktopBoxCentersOnObjectUsingSourcesizeFrame()
        {
            // desktop (142,157,112,104), ref (393,418):
            // center offset = (142+56-196.5, 157+52-209) = (1.5, 0); box = center - size/2.
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-54.5, -52, 112, 104), box);
        }

        /// <summary>Verifies that the star desktop hitbox aligns with the trimmed glow frame.</summary>
        [Fact]
        public void StarDesktopBoxUsesTrimmedGlowFrame()
        {
            // desktop (70,64,82,82), ref (236,223): offset = (-7, -6.5).
            LevelBounds? box = HitboxTable.Compute("star", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-48, -47.5, 82, 82), box);
        }

        /// <summary>Verifies that the target desktop hitbox maps to the mouth line.</summary>
        [Fact]
        public void TargetDesktopBoxIsTheMouthLine()
        {
            // desktop (264,350,108,2), ref (640,640): offset = (-2, 31).
            LevelBounds? box = HitboxTable.Compute("target", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-56, 30, 108, 2), box);
        }

        /// <summary>Verifies that phone hitboxes use WP7 scaling and differ from desktop bounds.</summary>
        [Fact]
        public void PhoneBoxScalesRawValuesByWp7AndDiffersFromDesktop()
        {
            // phone (46,49,35,35)*3 = (138,147,105,105), ref (393,418):
            // offset = (138+52.5-196.5, 147+52.5-209) = (-6, -9.5).
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 3, HitboxModel.Phone);
            Assert.Equal(new LevelBounds(-58.5, -62, 105, 105), box);
        }

        /// <summary>Verifies that object coordinates translate the computed hitbox.</summary>
        [Fact]
        public void ObjectPositionTranslatesTheBox()
        {
            LevelBounds? box = HitboxTable.Compute("candy", 100, 200, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(45.5, 148, 112, 104), box);
        }

        /// <summary>Verifies that per-object scale shrinks the hitbox around the object center.</summary>
        [Fact]
        public void ObjectScaleShrinksTheBoxAboutTheObjectCenter()
        {
            // candy scale 0.71, s = 0.71/3. Width = 112 * s; center stays near (0,0).
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 0.71, HitboxModel.Desktop);
            double s = 0.71 / 3.0;
            _ = Assert.NotNull(box);
            Assert.Equal(112 * s, box.Value.W, precision: 9);
            Assert.Equal(104 * s, box.Value.H, precision: 9);
            Assert.Equal(1.5 * s, box.Value.X + (box.Value.W / 2.0), precision: 9); // center x
            Assert.Equal(0.0, box.Value.Y + (box.Value.H / 2.0), precision: 9); // center y
        }

        /// <summary>Verifies that unsupported elements do not produce hitboxes.</summary>
        [Theory]
        [InlineData("grab")]
        [InlineData("bubble")]
        [InlineData("")]
        public void UnsupportedElementsReturnNull(string element)
        {
            Assert.Null(HitboxTable.Compute(element, 0, 0, scale: 3, HitboxModel.Desktop));
        }
    }
}

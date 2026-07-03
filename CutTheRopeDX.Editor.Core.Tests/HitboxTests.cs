using CutTheRopeDX.Editor.Core.Editing;
using CutTheRopeDX.Editor.Core.Geometry;

using Xunit;

namespace CutTheRopeDX.Editor.Core.Tests
{
    public class HitboxTests
    {
        // scale = 3 makes s = scale/mapScale = 1, so level units == game px for clean expectations.

        [Fact]
        public void Candy_desktop_box_centers_on_object_using_sourcesize_frame()
        {
            // desktop (142,157,112,104), ref (393,418):
            // center offset = (142+56-196.5, 157+52-209) = (1.5, 0); box = center - size/2.
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-54.5, -52, 112, 104), box);
        }

        [Fact]
        public void Star_desktop_box_uses_trimmed_glow_frame()
        {
            // desktop (70,64,82,82), ref (236,223): offset = (-7, -6.5).
            LevelBounds? box = HitboxTable.Compute("star", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-48, -47.5, 82, 82), box);
        }

        [Fact]
        public void Target_desktop_box_is_the_mouth_line()
        {
            // desktop (264,350,108,2), ref (640,640): offset = (-2, 31).
            LevelBounds? box = HitboxTable.Compute("target", 0, 0, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(-56, 30, 108, 2), box);
        }

        [Fact]
        public void Phone_box_scales_raw_values_by_wp7_and_differs_from_desktop()
        {
            // phone (46,49,35,35)*3 = (138,147,105,105), ref (393,418):
            // offset = (138+52.5-196.5, 147+52.5-209) = (-6, -9.5).
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 3, HitboxModel.Phone);
            Assert.Equal(new LevelBounds(-58.5, -62, 105, 105), box);
        }

        [Fact]
        public void Object_position_translates_the_box()
        {
            LevelBounds? box = HitboxTable.Compute("candy", 100, 200, scale: 3, HitboxModel.Desktop);
            Assert.Equal(new LevelBounds(45.5, 148, 112, 104), box);
        }

        [Fact]
        public void Object_scale_shrinks_the_box_about_the_object_center()
        {
            // candy scale 0.71, s = 0.71/3. Width = 112 * s; center stays near (0,0).
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 0.71, HitboxModel.Desktop);
            double s = 0.71 / 3.0;
            _ = Assert.NotNull(box);
            Assert.Equal(112 * s, box!.Value.W, precision: 9);
            Assert.Equal(104 * s, box!.Value.H, precision: 9);
            Assert.Equal(1.5 * s, box!.Value.X + (box!.Value.W / 2.0), precision: 9); // center x
            Assert.Equal(0.0, box!.Value.Y + (box!.Value.H / 2.0), precision: 9); // center y
        }

        [Theory]
        [InlineData("grab")]
        [InlineData("bubble")]
        [InlineData("")]
        public void Unsupported_elements_return_null(string element)
        {
            Assert.Null(HitboxTable.Compute(element, 0, 0, scale: 3, HitboxModel.Desktop));
        }
    }
}

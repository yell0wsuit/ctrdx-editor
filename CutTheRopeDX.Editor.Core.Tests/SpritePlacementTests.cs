using CutTheRopeDX.Editor.Core.Atlas;
using CutTheRopeDX.Editor.Core.Editing;
using CutTheRopeDX.Editor.Core.Geometry;

using Xunit;

namespace CutTheRopeDX.Editor.Core.Tests
{
    public class SpritePlacementTests
    {
        // obj_hook_01_frame_0000: sourceSize 276x276, trim offset (78,76), frame 127x128.
        private static readonly AtlasFrame Hook = new(
            Filename: "obj_hook_01_frame_0000.png",
            Frame: new IntRect(921, 1, 127, 128),
            SpriteSource: new IntRect(78, 76, 127, 128),
            SourceSize: new IntSize(276, 276),
            Rotated: false, Trimmed: true);

        [Fact]
        public void Hit_box_is_untrimmed_sprite_centered_on_xy_scaled_by_mapscale()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);

            // 276 / 3 = 92; centered on (200,150) -> top-left (154,104).
            Assert.Equal(new LevelBounds(154, 104, 92, 92), layout.Hit);
        }

        [Fact]
        public void Source_is_the_atlas_pixel_rect()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);
            Assert.Equal(new IntRect(921, 1, 127, 128), layout.Source);
        }

        [Fact]
        public void Dest_offsets_the_trimmed_frame_by_the_trim_origin()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);

            // dest.X = 154 + 78/3 = 180 ; dest.W = 127/3
            Assert.Equal(180, layout.Dest.X, precision: 9);
            Assert.Equal(104 + (76.0 / 3.0), layout.Dest.Y, precision: 9);
            Assert.Equal(127.0 / 3.0, layout.Dest.W, precision: 9);
            Assert.Equal(128.0 / 3.0, layout.Dest.H, precision: 9);
        }

        [Fact]
        public void Per_object_scale_shrinks_the_sprite_about_its_center()
        {
            // candy scale 0.71: hit box = 276 * 0.71 / 3, still centered on (200,150).
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150, scale: 0.71);

            double w = 276.0 * 0.71 / 3.0;
            Assert.Equal(w, layout.Hit.W, precision: 9);
            Assert.Equal(w, layout.Hit.H, precision: 9);
            Assert.Equal(200, layout.Hit.X + (layout.Hit.W / 2.0), precision: 9);
            Assert.Equal(150, layout.Hit.Y + (layout.Hit.H / 2.0), precision: 9);
        }
    }
}

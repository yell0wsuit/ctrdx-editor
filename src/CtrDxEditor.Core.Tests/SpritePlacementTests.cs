using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for mapping atlas sprite frames into level-space bounds.</summary>
    public class SpritePlacementTests
    {
        // obj_hook_01_frame_0000: sourceSize 276x276, trim offset (78,76), frame 127x128.
        private static readonly AtlasFrame Hook = new(
            Filename: "obj_hook_01_frame_0000.png",
            Frame: new IntRect(921, 1, 127, 128),
            SpriteSource: new IntRect(78, 76, 127, 128),
            SourceSize: new IntSize(276, 276),
            Rotated: false, Trimmed: true);

        /// <summary>Verifies that hit bounds use the untrimmed sprite centered on object coordinates.</summary>
        [Fact]
        public void HitBoxIsUntrimmedSpriteCenteredOnXyScaledByMapscale()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);

            // 276 / 3 = 92; centered on (200,150) -> top-left (154,104).
            Assert.Equal(new LevelBounds(154, 104, 92, 92), layout.Hit);
        }

        /// <summary>Verifies that source bounds remain the original atlas pixel rectangle.</summary>
        [Fact]
        public void SourceIsTheAtlasPixelRect()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);
            Assert.Equal(new IntRect(921, 1, 127, 128), layout.Source);
        }

        /// <summary>Verifies that destination bounds apply the trim origin offset.</summary>
        [Fact]
        public void DestOffsetsTheTrimmedFrameByTheTrimOrigin()
        {
            SpriteLayout layout = SpritePlacement.Compute(Hook, x: 200, y: 150);

            // dest.X = 154 + 78/3 = 180 ; dest.W = 127/3
            Assert.Equal(180, layout.Dest.X, precision: 9);
            Assert.Equal(104 + (76.0 / 3.0), layout.Dest.Y, precision: 9);
            Assert.Equal(127.0 / 3.0, layout.Dest.W, precision: 9);
            Assert.Equal(128.0 / 3.0, layout.Dest.H, precision: 9);
        }

        /// <summary>
        /// A frame centered on itself ignores its trim origin, like a game Image that never restores cut
        /// transparency. obj_pause's face sits at x=607 of a 998-wide stage, so placing it by the trim
        /// would push it right of the object; centered, it straddles (x,y) exactly.
        /// </summary>
        [Fact]
        public void CenterOnFrameCentersTheTrimmedQuadOnXy()
        {
            AtlasFrame pauseFace = new(
                Filename: "frame_0000.png",
                Frame: new IntRect(248, 697, 216, 215),
                SpriteSource: new IntRect(607, 641, 216, 215),
                SourceSize: new IntSize(998, 1498),
                Rotated: false, Trimmed: true);

            SpriteLayout layout = SpritePlacement.Compute(pauseFace, x: 200, y: 150, centerOnFrame: true);

            Assert.Equal(200 - (216.0 / 6.0), layout.Dest.X, precision: 9);
            Assert.Equal(150 - (215.0 / 6.0), layout.Dest.Y, precision: 9);
            Assert.Equal(216.0 / 3.0, layout.Dest.W, precision: 9);
            Assert.Equal(layout.Dest, layout.Hit);
        }

        /// <summary>Verifies that per-object scale shrinks the sprite about its center point.</summary>
        [Fact]
        public void PerObjectScaleShrinksTheSpriteAboutItsCenter()
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

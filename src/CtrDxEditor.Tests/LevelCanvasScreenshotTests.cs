using CtrDxEditor.Core.Editing;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the pure framing math behind the level-screenshot export.</summary>
    public class LevelCanvasScreenshotTests
    {
        /// <summary>No background: the frame is the playfield, rendered at MapScale, no pan.</summary>
        [Fact]
        public void NoBackgroundUsesLevelSizeAtMapScale()
        {
            LevelCanvas.ScreenshotFrame frame = LevelCanvas.ComputeScreenshotFrame(640, 480, 0);

            Assert.Equal(1920, frame.Size.Width);
            Assert.Equal(1440, frame.Size.Height);
            Assert.Equal(SpritePlacement.MapScale, frame.View.Zoom);
            Assert.Equal(0, frame.View.PanX, 3);
            Assert.Equal(0, frame.View.PanY, 3);
        }

        /// <summary>Background wider than the level: frame widens to the bg column and centers the level.</summary>
        [Fact]
        public void WideBackgroundUsesBackgroundWidthCentered()
        {
            double bg = BackgroundPlacement.LevelScreenWidth;

            LevelCanvas.ScreenshotFrame frame = LevelCanvas.ComputeScreenshotFrame(640, 480, bg);

            Assert.Equal(2560, frame.Size.Width);
            Assert.Equal(1440, frame.Size.Height);
            // Level narrower than the column -> left wing (bg-640)/2 level units, xMapScale in screen px.
            double expectedPanX = (bg - 640) / 2.0 * SpritePlacement.MapScale;
            Assert.Equal(expectedPanX, frame.View.PanX, 3);
        }

        /// <summary>Level wider than the background column: frame follows the level width, no pan.</summary>
        [Fact]
        public void WideLevelUsesLevelWidth()
        {
            double bg = BackgroundPlacement.LevelScreenWidth;

            LevelCanvas.ScreenshotFrame frame = LevelCanvas.ComputeScreenshotFrame(1200, 500, bg);

            Assert.Equal(3600, frame.Size.Width);
            Assert.Equal(1500, frame.Size.Height);
            Assert.Equal(0, frame.View.PanX, 3);
        }
    }
}

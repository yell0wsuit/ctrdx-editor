using System.Reflection;

using Avalonia;

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

        /// <summary>Timed star labels sit slightly inside the star top instead of floating above it.</summary>
        [Fact]
        public void StarDurationLabelSitsBelowStarTop()
        {
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "ComputeStarDurationOrigin",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Point origin = (Point)method.Invoke(null, [new Point(120, 80), new Size(20, 12), 2.0])!;

            Assert.Equal(110, origin.X, 3);
            Assert.Equal(84, origin.Y, 3);
        }

        /// <summary>Timed star labels include the seconds unit without changing decimal trimming.</summary>
        [Theory]
        [InlineData(5.0, "5s")]
        [InlineData(4.5, "4.5s")]
        public void StarDurationLabelShowsSecondsUnit(double timeout, string expected)
        {
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "FormatStarDuration",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string label = (string)method.Invoke(null, [timeout])!;

            Assert.Equal(expected, label);
        }
    }
}

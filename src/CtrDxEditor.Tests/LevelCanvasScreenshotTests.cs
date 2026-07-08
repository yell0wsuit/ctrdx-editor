using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the pure framing math behind the level-screenshot export.</summary>
    public class LevelCanvasScreenshotTests
    {
        private sealed class FakeStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(true);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult<byte[]>([]);
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult(/*lang=json,strict*/ """{"frames":{}}""");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }
        }

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

        /// <summary>Night levels keep normal stars but draw classic sleeping Om Nom.</summary>
        [Theory]
        [InlineData("star", true, "star")]
        [InlineData("target", true, "target_sleeping")]
        [InlineData("star", false, "star")]
        [InlineData("target", false, "target")]
        public void CanvasSpriteKeyUsesNightLevelVariants(string element, bool nightLevel, string expected)
        {
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "CanvasSpriteKey",
                BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(string), typeof(bool)]);
            Assert.NotNull(method);

            string key = (string)method.Invoke(null, [element, nightLevel])!;

            Assert.Equal(expected, key);
        }

        /// <summary>Night star selection keeps the normal star marquee instead of the smaller night atlas canvas.</summary>
        [Fact]
        public void NightStarSelectionBoundsUseNormalStarBounds()
        {
            SpriteCache sprites = SeedStarAtlases();
            LevelObject star = new(new XElement("star", new XAttribute("x", "0"), new XAttribute("y", "0")));
            MethodInfo? method = typeof(LevelCanvas).GetMethod(
                "SelectionBounds",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            LevelBounds normal = (LevelBounds)method.Invoke(null, [sprites, star, 0, 0, false])!;
            LevelBounds night = (LevelBounds)method.Invoke(null, [sprites, star, 0, 0, true])!;

            Assert.Equal(normal.X, night.X, 3);
            Assert.Equal(normal.Y, night.Y, 3);
            Assert.Equal(normal.W, night.W, 3);
            Assert.Equal(normal.H, night.H, 3);
        }

        private static SpriteCache SeedStarAtlases()
        {
            SpriteCache cache = new(new FakeStore());
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/obj_star_idle.png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/obj_star_idle.json"] = new Atlas(
                [
                    Frame("idle-glow", 236, 223, 155, 155, 552, 552),
                    .. EmptyFrames(17),
                    Frame("idle-body", 85, 80, 229, 229, 552, 552),
                ]),
            });
            return cache;
        }

        private static IEnumerable<AtlasFrame> EmptyFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Frame($"empty-{i}", 1, 1, 0, 0, 1, 1);
            }
        }

        private static AtlasFrame Frame(string filename, int w, int h, int sourceX, int sourceY, int sourceW, int sourceH)
        {
            return new AtlasFrame(
                filename,
                new IntRect(0, 0, w, h),
                new IntRect(sourceX, sourceY, w, h),
                new IntSize(sourceW, sourceH),
                Rotated: false,
                Trimmed: true);
        }

        private static void SetPrivateField<T>(SpriteCache cache, string fieldName, T value)
        {
            FieldInfo field = typeof(SpriteCache).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(cache, value);
        }
    }
}

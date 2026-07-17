using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Media;
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
        // The scene-drawing helpers live on the internal LevelSceneRenderer; reflect into it by name
        // since the test assembly can't reference the internal type directly.
        private static readonly Type SceneRenderer =
            typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

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
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeStarDurationOrigin",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
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
            MethodInfo? method = SceneRenderer.GetMethod(
                "FormatStarDuration",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string label = (string)method.Invoke(null, [timeout])!;

            Assert.Equal(expected, label);
        }

        /// <summary>
        /// A rocket's burn-time countdown reads the positive <c>time</c> attribute; the default -1 (fires
        /// until impact) and an absent value read as 0-or-negative so no countdown label is drawn.
        /// </summary>
        [Theory]
        [InlineData("5", 5.0)]
        [InlineData("4.5", 4.5)]
        [InlineData("-1", -1.0)]
        [InlineData(null, 0.0)]
        public void RocketTimeReadsBurnSeconds(string? time, double expected)
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "RocketTime",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            XElement e = new("rocket");
            if (time is not null)
            {
                e.SetAttributeValue("time", time);
            }
            LevelObject rocket = new(e);

            double value = (double)method.Invoke(null, [rocket])!;

            Assert.Equal(expected, value, 3);
        }

        /// <summary>Night levels keep normal stars but draw classic sleeping Om Nom.</summary>
        [Theory]
        [InlineData("star", true, "star")]
        [InlineData("target", true, "target_sleeping")]
        [InlineData("star", false, "star")]
        [InlineData("target", false, "target")]
        public void CanvasSpriteKeyUsesNightLevelVariants(string element, bool nightLevel, string expected)
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "CanvasSpriteKey",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(string), typeof(bool)]);
            Assert.NotNull(method);

            string key = (string)method.Invoke(null, [element, nightLevel])!;

            Assert.Equal(expected, key);
        }

        /// <summary>Magic-hat canvas keys combine the current Christmas event with the authored group.</summary>
        [Theory]
        [InlineData("0", false, "sock")]
        [InlineData("2", false, "sock_grouped")]
        [InlineData("0", true, "sock_xmas")]
        [InlineData("2", true, "sock_xmas_grouped")]
        public void CanvasSpriteKeyUsesSockSeasonAndGroup(string group, bool isXmas, string expected)
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "CanvasSpriteKey",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(LevelObject), typeof(bool), typeof(bool)]);
            Assert.NotNull(method);
            LevelObject sock = new(new XElement("sock", new XAttribute("group", group)));

            string key = (string)method.Invoke(null, [sock, false, isXmas])!;

            Assert.Equal(expected, key);
        }

        /// <summary>Night star selection keeps the normal star marquee instead of the smaller night atlas canvas.</summary>
        [Fact]
        public void NightStarSelectionBoundsUseNormalStarBounds()
        {
            SpriteCache sprites = SeedStarAtlases();
            LevelObject star = new(new XElement("star", new XAttribute("x", "0"), new XAttribute("y", "0")));
            MethodInfo? method = SceneRenderer.GetMethod(
                "SelectionBounds",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            LevelBounds normal = (LevelBounds)method.Invoke(null, [sprites, star, 0, 0, false])!;
            LevelBounds night = (LevelBounds)method.Invoke(null, [sprites, star, 0, 0, true])!;

            Assert.Equal(normal.X, night.X, 3);
            Assert.Equal(normal.Y, night.Y, 3);
            Assert.Equal(normal.W, night.W, 3);
            Assert.Equal(normal.H, night.H, 3);
        }

        /// <summary>A hand's selection box encloses its full articulated chain rather than only the base sprite.</summary>
        [Fact]
        public void HandSelectionBoundsUseArticulatedChain()
        {
            SpriteCache sprites = new(new FakeStore());
            LevelObject hand = new(new XElement(
                "hand",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("segmentsCount", "2"),
                new XAttribute("segment1Angle", "0"),
                new XAttribute("segment1Length", "100"),
                new XAttribute("segment1Rotatable", "true"),
                new XAttribute("segment2Angle", "90"),
                new XAttribute("segment2Length", "50"),
                new XAttribute("segment2Rotatable", "true")));
            MethodInfo? method = SceneRenderer.GetMethod(
                "SelectionBounds",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            LevelBounds actual = (LevelBounds)method.Invoke(null, [sprites, hand, 0, 0, false])!;
            LevelBounds expected = HandGeometry.Bounds(hand);

            Assert.Equal(expected.X, actual.X, 6);
            Assert.Equal(expected.Y, actual.Y, 6);
            Assert.Equal(expected.W, actual.W, 6);
            Assert.Equal(expected.H, actual.H, 6);
        }

        /// <summary>Spike selection uses the trimmed visible strip, like other sprite-backed objects.</summary>
        [Fact]
        public void SpikeSelectionBoundsUseTrimmedSpriteBounds()
        {
            SpriteCache sprites = SeedSpikeAtlas();
            LevelObject spike = new(new XElement("spike4", new XAttribute("x", "100"), new XAttribute("y", "200"), new XAttribute("size", "4")));
            MethodInfo? method = SceneRenderer.GetMethod(
                "SelectionBounds",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            LevelBounds bounds = (LevelBounds)method.Invoke(null, [sprites, spike, 0, 0, false])!;

            Assert.Equal(-18.5, bounds.X, 3);
            Assert.Equal(179.375, bounds.Y, 3);
            Assert.Equal(236.667, bounds.W, 3);
            Assert.Equal(39.583, bounds.H, 3);
        }

        /// <summary>The selected spike outline rotates with the object's angle instead of staying axis-aligned.</summary>
        [Fact]
        public void SpikeSelectionOutlineRotatesWithObject()
        {
            LevelBounds bounds = new(-18.5, 179.375, 236.667, 39.583);
            LevelObject spike = new(new XElement(
                "spike4",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("angle", "90"),
                new XAttribute("size", "4")));
            MethodInfo? method = SceneRenderer.GetMethod(
                "SelectionOutlinePoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Point[] points = (Point[])method.Invoke(null, [ViewTransform.Identity, spike, bounds])!;

            Assert.Equal(120.625, points[0].X, 3);
            Assert.Equal(81.5, points[0].Y, 3);
            Assert.Equal(120.625, points[1].X, 3);
            Assert.Equal(318.167, points[1].Y, 3);
        }

        /// <summary>
        /// The magic hat's selection marquee sits below the object anchor, matching the game, which draws the
        /// hat sprite offset from its (collision) anchor. A naive center-anchored hat would sit above it.
        /// </summary>
        [Fact]
        public void SockSelectionBoundsSitBelowAnchorByGameOffset()
        {
            SpriteCache sprites = SeedHatAtlases();
            LevelObject sock = new(new XElement("sock", new XAttribute("x", "0"), new XAttribute("y", "0")));
            MethodInfo? method = SceneRenderer.GetMethod(
                "SelectionBounds",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            LevelBounds bounds = (LevelBounds)method.Invoke(null, [sprites, sock, 0, 0, false])!;

            // The visible sprite center should be the game's downward offset applied to the same frame.
            double offsetY = SockSprite.DrawOffsetY(431, 0.7);
            LevelBounds dest = SpritePlacement.Compute(Frame("frame_0000.png", 296, 337, 52, 5, 431, 431), 0, offsetY, 0.7).Dest;
            Assert.Equal(dest.Y + (dest.H / 2.0), bounds.Y + (bounds.H / 2.0), precision: 6);
            Assert.True(bounds.Y + (bounds.H / 2.0) > 0, "hat marquee should sit below the anchor");
        }

        /// <summary>Spin arrows use the rotateSpeed sign: positive sweeps clockwise, negative counter-clockwise.</summary>
        [Fact]
        public void SpinArrowDirectionFollowsRotateSpeedSign()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeSpinArrowPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject clockwise = new(new XElement("star", new XAttribute("x", "100"), new XAttribute("y", "200"), new XAttribute("rotateSpeed", "70")));
            LevelObject counterClockwise = new(new XElement("star", new XAttribute("x", "100"), new XAttribute("y", "200"), new XAttribute("rotateSpeed", "-70")));

            Point[] cw = (Point[])method.Invoke(null, [ViewTransform.Identity, clockwise, 20.0])!;
            Point[] ccw = (Point[])method.Invoke(null, [ViewTransform.Identity, counterClockwise, 20.0])!;

            Assert.Equal(cw[0].X, ccw[0].X, 3);
            Assert.Equal(cw[0].Y, ccw[0].Y, 3);
            Assert.True(cw[1].Y < cw[0].Y);
            Assert.True(ccw[1].Y > ccw[0].Y);
        }

        /// <summary>The candy crosshair centers on the object and keeps fixed screen-space arms at any zoom.</summary>
        [Fact]
        public void CandyCrosshairCentersOnObjectInScreenSpace()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeCandyCrosshairPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject candy = new(new XElement("candy", new XAttribute("x", "100"), new XAttribute("y", "200")));

            // Zoomed 3x: the center moves with the transform but the 6px arms stay screen-fixed.
            Point[] points = (Point[])method.Invoke(null, [new ViewTransform(3.0, 0.0, 0.0), candy, 6.0])!;

            Assert.Equal(4, points.Length);
            Assert.Equal(new Point(300 - 6, 600), points[0]);
            Assert.Equal(new Point(300 + 6, 600), points[1]);
            Assert.Equal(new Point(300, 600 - 6), points[2]);
            Assert.Equal(new Point(300, 600 + 6), points[3]);
        }

        /// <summary>Live orbit preview moves hitbox bounds with the object position, matching DX mover updates.</summary>
        [Fact]
        public void OrbitPreviewHitboxFollowsPreviewPosition()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "PreviewHitboxBounds",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject star = new(new XElement(
                "star",
                new XAttribute("x", "100"),
                new XAttribute("y", "100"),
                new XAttribute("path", "RC8"),
                new XAttribute("moveSpeed", "8")));

            LevelBounds authored = (LevelBounds)method.Invoke(null, [star, 1.0, HitboxModel.Desktop, null])!;
            LevelBounds preview = (LevelBounds)method.Invoke(null, [star, 1.0, HitboxModel.Desktop, 0.0])!;

            Assert.Equal(authored.X + 8.0, preview.X, 6);
            Assert.Equal(authored.Y, preview.Y, 6);
            Assert.Equal(authored.W, preview.W, 6);
            Assert.Equal(authored.H, preview.H, 6);
        }

        /// <summary>The orbit path overlay is a circle centered on the authored object position.</summary>
        [Fact]
        public void OrbitPathPointsCircleAuthoredCenter()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeOrbitPathPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject star = new(new XElement(
                "star",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("path", "RC30"),
                new XAttribute("moveSpeed", "70")));

            Point[] points = (Point[])method.Invoke(null, [ViewTransform.Identity, star])!;

            Assert.True(points.Length > 12);
            Assert.Equal(130.0, points[0].X, 6);
            Assert.Equal(200.0, points[0].Y, 6);
            Assert.All(points, p =>
            {
                double dx = p.X - 100.0;
                double dy = p.Y - 200.0;
                Assert.Equal(30.0, Math.Sqrt((dx * dx) + (dy * dy)), 6);
            });
        }

        /// <summary>Plain movement path overlays follow DX's authored start plus relative point offsets.</summary>
        [Fact]
        public void MovementPathPointsFollowPlainDxOffsets()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeMovementPathPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject star = new(new XElement(
                "star",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("path", "100,0,100,50"),
                new XAttribute("moveSpeed", "70")));

            Point[] points = (Point[])method.Invoke(null, [ViewTransform.Identity, star])!;

            Assert.Equal(3, points.Length);
            Assert.Equal(new Point(100, 200), points[0]);
            Assert.Equal(new Point(200, 200), points[1]);
            Assert.Equal(new Point(200, 250), points[2]);
        }

        /// <summary>The orbit direction arrow sits on the circle tangent and follows RC/RW direction.</summary>
        [Fact]
        public void OrbitArrowDirectionFollowsPathPrefix()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeOrbitArrowPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            LevelObject clockwise = new(new XElement(
                "star",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("path", "RC30"),
                new XAttribute("moveSpeed", "70")));
            LevelObject counterClockwise = new(new XElement(
                "star",
                new XAttribute("x", "100"),
                new XAttribute("y", "200"),
                new XAttribute("path", "RW30"),
                new XAttribute("moveSpeed", "70")));

            Point[] cw = (Point[])method.Invoke(null, [ViewTransform.Identity, clockwise])!;
            Point[] ccw = (Point[])method.Invoke(null, [ViewTransform.Identity, counterClockwise])!;

            Assert.Equal(4, cw.Length);
            Assert.Equal(4, ccw.Length);
            Assert.True(cw[1].X > cw[0].X);
            Assert.Equal(cw[0].Y, cw[1].Y, 6);
            Assert.True(ccw[1].X < ccw[0].X);
            Assert.Equal(ccw[0].Y, ccw[1].Y, 6);
        }

        /// <summary>The orbit path overlay uses dots, not the longer shared overlay dash pattern.</summary>
        [Fact]
        public void OrbitPathPenUsesDottedPattern()
        {
            Type paletteType = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.CanvasPalette")!;
            object palette = Activator.CreateInstance(paletteType, nonPublic: true)!;
            Pen pen = (Pen)paletteType.GetProperty("OrbitPath")!.GetValue(palette)!;

            Assert.NotNull(pen.DashStyle);
            Assert.Equal([1.0, 3.0], pen.DashStyle!.Dashes);
        }

        /// <summary>The orbit direction arrow is solid so its small head remains legible over the dotted path.</summary>
        [Fact]
        public void OrbitPathArrowPenIsSolid()
        {
            Type paletteType = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.CanvasPalette")!;
            object palette = Activator.CreateInstance(paletteType, nonPublic: true)!;
            Pen pen = (Pen)paletteType.GetProperty("OrbitPathArrow")!.GetValue(palette)!;

            Assert.Null(pen.DashStyle);
            Assert.True(pen.Thickness > 1.5);
        }

        /// <summary>The candy hazard-alert crosshair pen is solid red, distinct from the normal solid pen.</summary>
        [Fact]
        public void CandyCrosshairAlertPenIsSolid()
        {
            Type paletteType = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.CanvasPalette")!;
            object palette = Activator.CreateInstance(paletteType, nonPublic: true)!;
            Pen pen = (Pen)paletteType.GetProperty("CandyCrosshairAlert")!.GetValue(palette)!;

            Assert.Null(pen.DashStyle);
            Assert.True(pen.Thickness > 1.0);
        }

        /// <summary>Steam force ticks cross the shaft at the game's low, medium, and maximum levels.</summary>
        [Fact]
        public void SteamForceLevelMarksUseExactRotatedEndpoints()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeForceLevelMarkPoints",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Point[] up = (Point[])method.Invoke(null,
                [ViewTransform.Identity, new Vec2(100, 200), -Math.PI / 2, new double[] { 32.9, 94, 141 }, 10.0])!;
            Assert.Equal(6, up.Length);
            Assert.Equal(new Point(95, 167.1), up[0]);
            Assert.Equal(new Point(105, 167.1), up[1]);
            Assert.Equal(new Point(95, 106), up[2]);
            Assert.Equal(new Point(105, 106), up[3]);
            Assert.Equal(95, up[4].X, 6);
            Assert.Equal(59, up[4].Y, 6);
            Assert.Equal(105, up[5].X, 6);
            Assert.Equal(59, up[5].Y, 6);

            Point[] right = (Point[])method.Invoke(null,
                [ViewTransform.Identity, new Vec2(100, 200), 0.0, new double[] { 141 }, 10.0])!;
            Assert.Equal(new Point(241, 195), right[0]);
            Assert.Equal(new Point(241, 205), right[1]);
        }

        /// <summary>Steam's body is behind Ghost, which is behind the grab pass, matching GameScene.Draw.</summary>
        [Fact]
        public void SteamTubeDrawLayerMatchesGameOrder()
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "GameDrawLayer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
            static LevelObject Obj(string type)
            {
                return new LevelObject(new XElement(type));
            }

            int steam = (int)method.Invoke(null, [Obj("steamTube")])!;
            int ghost = (int)method.Invoke(null, [Obj("ghost")])!;
            int grab = (int)method.Invoke(null, [Obj("grab")])!;

            Assert.True(steam < ghost);
            Assert.True(ghost < grab);
        }

        /// <summary>Tube art and valve follow SteamTube's exact local offsets and parent rotation.</summary>
        [Fact]
        public void SteamTubePartCentersMatchGameTransform()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeSteamTubePartCenters",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Vec2[] upright = (Vec2[])method.Invoke(null, [new Vec2(100, 200), 0.0])!;
            Assert.Equal(2, upright.Length);
            Assert.Equal(100, upright[0].X, 6);
            Assert.Equal(200 + 28, upright[0].Y, 6);
            Assert.Equal(new Vec2(100, 227), upright[1]);

            Vec2[] right = (Vec2[])method.Invoke(null, [new Vec2(100, 200), 90.0])!;
            Assert.Equal(100 - 28, right[0].X, 6);
            Assert.Equal(200, right[0].Y, 6);
            Assert.Equal(73, right[1].X, 6);
            Assert.Equal(200, right[1].Y, 6);
        }

        /// <summary>Every frozen puff's local plume position rotates with the SteamTube parent.</summary>
        [Fact]
        public void SteamPuffCentersRotateWithPipe()
        {
            MethodInfo? method = SceneRenderer.GetMethod(
                "ComputeSteamPuffCenter",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            SteamPuffSpec puff = SteamTubeGeometry.MaximumPlume()[0];

            Vec2 upright = (Vec2)method.Invoke(null, [new Vec2(100, 200), 0.0, puff])!;
            Vec2 right = (Vec2)method.Invoke(null, [new Vec2(100, 200), 90.0, puff])!;

            Assert.Equal(100 + puff.LocalX, upright.X, 6);
            Assert.Equal(200 + puff.LocalY, upright.Y, 6);
            Assert.Equal(100 - puff.LocalY, right.X, 6);
            Assert.Equal(200 + puff.LocalX, right.Y, 6);
        }

        /// <summary>The static plume is faded to reduce canvas clutter without fading pipe hardware.</summary>
        [Fact]
        public void SteamPuffOpacityIsFiftyFivePercent()
        {
            FieldInfo? field = SceneRenderer.GetField(
                "SteamPuffOpacity",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(field);
            Assert.Equal(0.55, (double)field.GetRawConstantValue()!, 6);
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

        private static SpriteCache SeedSpikeAtlas()
        {
            SpriteCache cache = new(new FakeStore());
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/obj_spikes.png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/obj_spikes.json"] = new Atlas(
                [
                    .. EmptyFrames(11),
                    Frame("obj_spikes_04_frame_0000.png", 568, 95, 132, 75, 833, 250),
                ]),
            });
            return cache;
        }

        private static SpriteCache SeedHatAtlases()
        {
            SpriteCache cache = new(new FakeStore());
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/obj_hat.png"] = bitmap,
                ["images/obj_sock_xmas.png"] = bitmap,
            });
            // Both seasonal atlases share the hat's 431x431 source frame so the test is season-independent.
            AtlasFrame hat = Frame("frame_0000.png", 296, 337, 52, 5, 431, 431);
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/obj_hat.json"] = new Atlas([hat]),
                ["images/obj_sock_xmas.json"] = new Atlas([hat]),
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

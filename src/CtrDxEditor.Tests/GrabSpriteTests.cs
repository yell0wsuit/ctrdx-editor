using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the grab hook sprites: the fixed hook and the auto-catch (auto-hook) variant.</summary>
    public class GrabSpriteTests
    {
        /// <summary>The fixed grab uses the normal hook frames (game quads 0/1).</summary>
        [Fact]
        public void FixedGrabUsesFixedHookFrames()
        {
            VisualDescriptor grab = VisualDescriptorMap.For("grab")!;

            Assert.Equal(
                ["obj_hook_01_frame_0000.png", "obj_hook_01_frame_0001.png"],
                grab.Layers.Select(l => l.FrameName));
        }

        /// <summary>The auto-catch grab uses the auto-hook frames (game HookAuto quads 4/5).</summary>
        [Fact]
        public void AutoCatchGrabUsesAutoHookFrames()
        {
            VisualDescriptor grabAuto = VisualDescriptorMap.For("grab_auto")!;

            Assert.Equal(
                ["obj_hook_auto_frame_0000.png", "obj_hook_auto_frame_0001.png"],
                grabAuto.Layers.Select(l => l.FrameName));
            Assert.All(grabAuto.Layers, l => Assert.Equal("images/obj_hook.json", l.AtlasJsonRelPath));
        }

        /// <summary>The rail is assembled from the movable left cap, center tile, and right cap, in that order.</summary>
        [Fact]
        public void MovableRailUsesCapAndTileFrames()
        {
            VisualDescriptor rail = VisualDescriptorMap.For("grab_rail")!;

            Assert.Equal(
                [
                    "obj_hook_movable_frame_0000.png", // left cap
                    "obj_hook_movable_frame_0002.png", // center tile
                    "obj_hook_movable_frame_0001.png", // right cap
                ],
                rail.Layers.Select(l => l.FrameName));
        }

        /// <summary>The movable hook uses the movable hook frame (game HookMovable quad 10).</summary>
        [Fact]
        public void MovableGrabUsesMovableHookFrame()
        {
            VisualDescriptor movable = VisualDescriptorMap.For("grab_movable")!;

            Assert.Equal(["obj_hook_movable_frame_0004.png"], movable.Layers.Select(l => l.FrameName));
        }

        /// <summary>The dragged movable hook uses the highlight frame (game HookMovable quad 9).</summary>
        [Fact]
        public void MovableGrabHighlightUsesHighlightFrame()
        {
            VisualDescriptor highlight = VisualDescriptorMap.For("grab_movable_highlight")!;

            Assert.Equal(["obj_hook_movable_frame_0003.png"], highlight.Layers.Select(l => l.FrameName));
        }

        /// <summary>Wheel grabs use the regulated hook wheel frames.</summary>
        [Fact]
        public void WheelGrabUsesRegulatedWheelFrames()
        {
            VisualDescriptor wheel = VisualDescriptorMap.For("grab_wheel")!;

            Assert.Equal(
                [
                    "obj_hook_regulated_frame_0000.png",
                    "obj_hook_regulated_frame_0001.png",
                    "obj_hook_regulated_frame_0003.png",
                ],
                wheel.Layers.Select(l => l.FrameName));
        }

        /// <summary>Gun grabs use the gun back, arrow, and front frames.</summary>
        [Fact]
        public void GunGrabUsesGunFrames()
        {
            VisualDescriptor gun = VisualDescriptorMap.For("grab_gun")!;

            Assert.Equal(
                [
                    "frame_00_GunBackQuad.png",
                    "frame_01_GunArrowQuad.png",
                    "frame_02_GunFrontQuad.png",
                ],
                gun.Layers.Select(l => l.FrameName));
            Assert.All(gun.Layers, l => Assert.Equal("images/obj_gun.json", l.AtlasJsonRelPath));
        }

        /// <summary>Spider overlays use the initial dormant frame from the spider atlas.</summary>
        [Fact]
        public void SpiderGrabUsesInitialDormantFrame()
        {
            VisualDescriptor spider = VisualDescriptorMap.For("grab_spider")!;

            SpriteLayer layer = Assert.Single(spider.Layers);
            Assert.Equal("images/obj_spider.json", layer.AtlasJsonRelPath);
            Assert.Equal("frame_0000.png", layer.FrameName);
        }

        /// <summary>Suction cup grabs use attached or detached sticker frames depending on kicked state.</summary>
        [Theory]
        [InlineData("grab_suction", "frame_0003.png", "frame_0004.png")]
        [InlineData("grab_suction_kicked", "frame_0001.png", "frame_0002.png")]
        public void SuctionCupGrabsUseStickerFrames(string key, string backFrame, string frontFrame)
        {
            VisualDescriptor suction = VisualDescriptorMap.For(key)!;

            Assert.Equal([backFrame, frontFrame], suction.Layers.Select(l => l.FrameName));
            Assert.All(suction.Layers, l => Assert.Equal("images/obj_sticker.json", l.AtlasJsonRelPath));
        }

        /// <summary>Grab sprite selection follows the active grab variant attributes.</summary>
        [Theory]
        [InlineData("""<grab x="0" y="0" wheel="true" />""", "grab_wheel")]
        [InlineData("""<grab x="0" y="0" gun="true" />""", "grab_gun")]
        [InlineData("""<grab x="0" y="0" kickable="true" kicked="false" />""", "grab_suction")]
        [InlineData("""<grab x="0" y="0" kickable="true" kicked="true" />""", "grab_suction_kicked")]
        public void GrabSpriteKeyUsesVariantAttributes(string xml, string expectedKey)
        {
            Assert.Equal(expectedKey, SpriteKey(new LevelObject(XElement.Parse(xml))));
        }

        /// <summary>Conflicting hook variants suppress stale movable rail data when rendering the canvas.</summary>
        [Theory]
        [InlineData("""<grab x="0" y="0" moveLength="100" wheel="true" />""")]
        [InlineData("""<grab x="0" y="0" moveLength="100" gun="true" />""")]
        [InlineData("""<grab x="0" y="0" moveLength="100" kickable="true" />""")]
        public void HookVariantsSuppressMovableRailRendering(string xml)
        {
            Assert.False(DrawsMovableRail(new LevelObject(XElement.Parse(xml))));
        }

        /// <summary>Ordinary grabs with positive moveLength still render as movable rails.</summary>
        [Fact]
        public void PlainMovableGrabRendersRail()
        {
            Assert.True(DrawsMovableRail(new LevelObject(XElement.Parse("""<grab x="0" y="0" moveLength="100" />"""))));
        }

        /// <summary>Spider is dormant overlay art and does not replace the grab hook itself.</summary>
        [Theory]
        [InlineData("""<grab x="0" y="0" spider="true" radius="-1" />""", "grab")]
        [InlineData("""<grab x="0" y="0" spider="true" radius="65" />""", "grab_auto")]
        public void SpiderGrabKeepsBaseHookSprite(string xml, string expectedKey)
        {
            LevelObject obj = new(XElement.Parse(xml));

            Assert.Equal(expectedKey, SpriteKey(obj));
            Assert.Equal(["grab_spider"], OverlaySpriteKeys(obj));
        }

        private static string SpriteKey(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "SpriteKey",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (string)method.Invoke(null, [obj])!;
        }

        private static string[] OverlaySpriteKeys(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "OverlaySpriteKeys",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return [.. (System.Collections.Generic.IEnumerable<string>)method.Invoke(null, [obj])!];
        }

        private static bool DrawsMovableRail(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "DrawsMovableRail",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (bool)method.Invoke(null, [obj])!;
        }
    }
}

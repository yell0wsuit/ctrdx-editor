using System;
using System.Collections.Generic;
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

            Assert.Equal([0, 1], grab.Layers.Select(l => l.Quad));
        }

        /// <summary>The second fixed-hook pair uses the alternate hook frames (game Hook02 quads 2/3).</summary>
        [Fact]
        public void SecondFixedGrabUsesAlternateHookFrames()
        {
            VisualDescriptor grab02 = VisualDescriptorMap.For("grab_02")!;

            Assert.Equal([2, 3], grab02.Layers.Select(l => l.Quad));
            Assert.All(grab02.Layers, l => Assert.Equal("images/obj_hook.json", l.AtlasJsonRelPath));
        }

        /// <summary>
        /// A plain fixed hook renders as one of the two random quad pairs (game RandomHookBaseQuad: 0/1 or
        /// 2/3), and the roll is stable for the object's lifetime so repaints never flicker.
        /// </summary>
        [Fact]
        public void PlainHookRendersOneRandomPairStablePerInstance()
        {
            LevelObject obj = new(XElement.Parse("""<grab x="0" y="0" radius="-1" />"""));

            string first = RenderSpriteKey(obj);

            Assert.True(first is "grab" or "grab_02");
            Assert.All(Enumerable.Range(0, 20), _ => Assert.Equal(first, RenderSpriteKey(obj)));
        }

        /// <summary>Every non-plain grab keeps its dedicated art; the random pair only applies to the fixed hook.</summary>
        [Theory]
        [InlineData("""<grab x="0" y="0" wheel="true" />""", "grab_wheel")]
        [InlineData("""<grab x="0" y="0" gun="true" />""", "grab_gun")]
        [InlineData("""<grab x="0" y="0" kickable="true" kicked="false" />""", "grab_suction")]
        [InlineData("""<grab x="0" y="0" kickable="true" kicked="true" />""", "grab_suction_kicked")]
        [InlineData("""<grab x="0" y="0" radius="65" />""", "grab_auto")]
        public void VariantHooksNeverRollToSecondPair(string xml, string expectedKey)
        {
            Assert.Equal(expectedKey, RenderSpriteKey(new LevelObject(XElement.Parse(xml))));
        }

        /// <summary>Across many placed hooks both random pairs appear, mirroring the game's per-load roll.</summary>
        [Fact]
        public void PlainHooksVaryBetweenBothPairs()
        {
            string[] keys =
            [
                .. Enumerable.Range(0, 100)
                    .Select(_ => RenderSpriteKey(new LevelObject(XElement.Parse("""<grab x="0" y="0" radius="-1" />"""))))
            ];

            Assert.True(keys.Distinct().Count() > 1);
        }

        /// <summary>The auto-catch grab uses the auto-hook frames (game HookAuto quads 4/5).</summary>
        [Fact]
        public void AutoCatchGrabUsesAutoHookFrames()
        {
            VisualDescriptor grabAuto = VisualDescriptorMap.For("grab_auto")!;

            Assert.Equal([4, 5], grabAuto.Layers.Select(l => l.Quad));
            Assert.All(grabAuto.Layers, l => Assert.Equal("images/obj_hook.json", l.AtlasJsonRelPath));
        }

        /// <summary>The rail is assembled from the movable left cap, center tile, and right cap, in that order.</summary>
        [Fact]
        public void MovableRailUsesCapAndTileFrames()
        {
            VisualDescriptor rail = VisualDescriptorMap.For("grab_rail")!;

            Assert.Equal([6, 8, 7], rail.Layers.Select(l => l.Quad));
        }

        /// <summary>The movable hook uses the movable hook frame (game HookMovable quad 10).</summary>
        [Fact]
        public void MovableGrabUsesMovableHookFrame()
        {
            VisualDescriptor movable = VisualDescriptorMap.For("grab_movable")!;

            Assert.Equal([10], movable.Layers.Select(l => l.Quad));
        }

        /// <summary>The dragged movable hook uses the highlight frame (game HookMovable quad 9).</summary>
        [Fact]
        public void MovableGrabHighlightUsesHighlightFrame()
        {
            VisualDescriptor highlight = VisualDescriptorMap.For("grab_movable_highlight")!;

            Assert.Equal([9], highlight.Layers.Select(l => l.Quad));
        }

        /// <summary>Wheel grabs use the regulated hook wheel frames.</summary>
        [Fact]
        public void WheelGrabUsesRegulatedWheelFrames()
        {
            VisualDescriptor wheel = VisualDescriptorMap.For("grab_wheel")!;

            Assert.Equal([11, 12, 14], wheel.Layers.Select(l => l.Quad));
        }

        /// <summary>Gun grabs use the gun back, arrow, and front frames.</summary>
        [Fact]
        public void GunGrabUsesGunFrames()
        {
            VisualDescriptor gun = VisualDescriptorMap.For("grab_gun")!;

            Assert.Equal([0, 1, 2], gun.Layers.Select(l => l.Quad));
            Assert.All(gun.Layers, l => Assert.Equal("images/obj_gun.json", l.AtlasJsonRelPath));
        }

        /// <summary>Spider overlays use the initial dormant frame from the spider atlas.</summary>
        [Fact]
        public void SpiderGrabUsesInitialDormantFrame()
        {
            VisualDescriptor spider = VisualDescriptorMap.For("grab_spider")!;

            SpriteLayer layer = Assert.Single(spider.Layers);
            Assert.Equal("images/obj_spider.json", layer.AtlasJsonRelPath);
            Assert.Equal(0, layer.Quad);
        }

        /// <summary>Suction cup grabs use attached or detached sticker frames depending on kicked state.</summary>
        [Theory]
        [InlineData("grab_suction", 3, 4)]
        [InlineData("grab_suction_kicked", 1, 2)]
        public void SuctionCupGrabsUseStickerFrames(string key, int backQuad, int frontQuad)
        {
            VisualDescriptor suction = VisualDescriptorMap.For(key)!;

            Assert.Equal([backQuad, frontQuad], suction.Layers.Select(l => l.Quad));
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

        /// <summary>Gun aim rotates around the gun toward the primary full candy, matching DX's star path.</summary>
        [Fact]
        public void GunAimRotationTargetsSingleFullCandy()
        {
            LevelObject candy = new(XElement.Parse("""<candy x="100" y="200" />"""));
            LevelObject grab = new(XElement.Parse("""<grab x="200" y="200" gun="true" />"""));

            Assert.Equal(0, GunAimRotationDegrees(grab, [candy, grab], twoParts: false));
        }

        /// <summary>Half-candy and multi-candy levels do not get gun aim targeting movement.</summary>
        [Theory]
        [InlineData(true, """<candy x="300" y="400" />""", """<grab x="0" y="0" gun="true" />""")]
        [InlineData(false, """<candy x="300" y="400" />""", """<candy x="350" y="400" />""", """<grab x="0" y="0" gun="true" />""")]
        public void GunAimRotationDisabledWhenDxCannotTargetPrimaryCandy(bool twoParts, params string[] xml)
        {
            LevelObject[] objects = [.. xml.Select(x => new LevelObject(XElement.Parse(x)))];
            LevelObject grab = objects.Single(o => o.Type == "grab");

            Assert.Null(GunAimRotationDegrees(grab, objects, twoParts));
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

        /// <summary>Movable spider grabs still place the dormant spider on the visible movable hook.</summary>
        [Fact]
        public void MovableSpiderOverlayAnchorsToMovableHook()
        {
            LevelObject obj = new(XElement.Parse("""<grab x="120" y="140" spider="true" moveLength="100" moveOffset="30" />"""));

            (double X, double Y) anchor = SpiderOverlayAnchor(obj);

            Assert.Equal((120, 140), anchor);
            Assert.Equal(["grab_spider"], OverlaySpriteKeys(obj));
            Assert.True(DrawsMovableRail(obj));
        }

        private static string SpriteKey(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "SpriteKey",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (string)method.Invoke(null, [obj])!;
        }

        private static string RenderSpriteKey(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "RenderSpriteKey",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (string)method.Invoke(null, [obj])!;
        }

        private static string[] OverlaySpriteKeys(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "OverlaySpriteKeys",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return [.. (IEnumerable<string>)method.Invoke(null, [obj])!];
        }

        private static bool DrawsMovableRail(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "DrawsMovableRail",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (bool)method.Invoke(null, [obj])!;
        }

        private static (double X, double Y) SpiderOverlayAnchor(LevelObject obj)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "SpiderOverlayAnchor",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            object anchor = method.Invoke(null, [obj])!;
            PropertyInfo x = anchor.GetType().GetProperty("X")!;
            PropertyInfo y = anchor.GetType().GetProperty("Y")!;
            return ((double)x.GetValue(anchor)!, (double)y.GetValue(anchor)!);
        }

        private static double? GunAimRotationDegrees(LevelObject grab, LevelObject[] objects, bool twoParts)
        {
            Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            MethodInfo method = grabRenderer.GetMethod(
                "GunAimRotationDegrees",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            return (double?)method.Invoke(null, [grab, objects, twoParts]);
        }
    }
}

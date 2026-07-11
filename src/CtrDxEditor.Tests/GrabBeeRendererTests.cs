using System;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests deterministic moving-grab bee and pollen behavior.</summary>
    public class GrabBeeRendererTests
    {
        /// <summary>DX's 44 world-pixel spacing is normalized by the editor's 3x map scale.</summary>
        [Fact]
        public void PollenUsesFortyFourUnitSpacing()
        {
            LevelObject grab = Obj("100,0", "50");

            Core.Geometry.Vec2[] particles = [.. GrabBeeRenderer.PollenPoints(grab)];

            Assert.Contains(particles, p => p.X == 0 && p.Y == 0);
            Assert.Contains(particles, p => Math.Abs(p.X - (44.0 / 3.0)) < 0.000001 && p.Y == 0);
            Assert.Contains(particles, p => Math.Abs(p.X - (88.0 / 3.0)) < 0.000001 && p.Y == 0);
        }

        /// <summary>Hidden paths suppress pollen while active movement still selects bee art.</summary>
        [Fact]
        public void HidePathSuppressesPollenButNotBee()
        {
            LevelObject grab = Obj("100,0", "50", hidePath: true);

            Assert.Empty(GrabBeeRenderer.PollenPoints(grab));
            Assert.True(GrabBeeRenderer.HasBee(grab));
        }

        /// <summary>Wing animation ping-pongs quads 2,3,4 at the game's 0.03-second frame delay.</summary>
        [Theory]
        [InlineData(null, "grab_bee_wing_1")]
        [InlineData(0.00, "grab_bee_wing_0")]
        [InlineData(0.03, "grab_bee_wing_1")]
        [InlineData(0.06, "grab_bee_wing_2")]
        [InlineData(0.09, "grab_bee_wing_1")]
        public void WingFramePingPongs(double? seconds, string expected)
        {
            Assert.Equal(expected, GrabBeeRenderer.WingSpriteKey(seconds));
        }

        /// <summary>A moving grab hides its rope only while its own animation preview is playing.</summary>
        [Theory]
        [InlineData("RC40", "50", null, true)]
        [InlineData("RC40", "50", 0.0, false)]
        [InlineData("100,0", "50", 1.0, false)]
        [InlineData("100,0", "0", 1.0, true)]
        public void RopeVisibilityFollowsActiveMovementPreview(
            string path,
            string moveSpeed,
            double? seconds,
            bool expected)
        {
            Assert.Equal(expected, GrabBeeRenderer.ShouldDrawRope(Obj(path, moveSpeed), seconds));
        }

        /// <summary>The bee sits above the carried hook using the fallback anchor from DX SetBee.</summary>
        [Fact]
        public void BeeAnchorLeavesCarriedHookVisible()
        {
            Core.Geometry.Vec2 anchor = GrabBeeRenderer.BeeAnchor(new Core.Geometry.Vec2(100, 100));

            Assert.Equal(98, anchor.X, precision: 6);
            Assert.Equal(100 - (58.0 / 3.0), anchor.Y, precision: 6);
        }

        /// <summary>Pollen uses the game's 1.5x quad size and independent deterministic axis scales.</summary>
        [Fact]
        public void PollenVisualUsesDxScaleAndAlphaRanges()
        {
            GrabBeeRenderer.PollenVisual visual = GrabBeeRenderer.PollenVisualAt(4, 0);

            Assert.Equal(1.5, GrabBeeRenderer.PollenQuadScale);
            Assert.InRange(visual.ScaleX, 0, 1);
            Assert.InRange(visual.ScaleY, 0, 1);
            Assert.NotEqual(visual.ScaleX, visual.ScaleY);
            Assert.InRange(visual.Alpha, 0.3, 1.0);
        }

        /// <summary>The deterministic preview advances each pollen component at DX's one-unit-per-second rate.</summary>
        [Fact]
        public void PollenVisualMovesOneUnitPerSecondTowardDxTargets()
        {
            GrabBeeRenderer.PollenVisual atZero = GrabBeeRenderer.PollenVisualAt(2, 0);
            GrabBeeRenderer.PollenVisual later = GrabBeeRenderer.PollenVisualAt(2, 0.1);

            Assert.InRange(Math.Abs(later.ScaleX - atZero.ScaleX), 0, 0.100001);
            Assert.InRange(Math.Abs(later.ScaleY - atZero.ScaleY), 0, 0.100001);
            Assert.InRange(Math.Abs(later.Alpha - atZero.Alpha), 0.099999, 0.100001);
        }

        /// <summary>The auto-catch radius ring follows the grab to its live preview position while it moves.</summary>
        [Fact]
        public void RadiusRingCenterFollowsMovingGrab()
        {
            LevelObject grab = Obj("100,0", "50");

            Core.Geometry.Vec2 moving = RadiusRingCenter(grab, 1.0);
            Core.Geometry.Vec2 authored = RadiusRingCenter(grab, null);

            Assert.Equal(50, moving.X, precision: 6);
            Assert.Equal(0, moving.Y, precision: 6);
            Assert.Equal(0, authored.X, precision: 6);
            Assert.Equal(0, authored.Y, precision: 6);
        }

        /// <summary>A stationary grab keeps its ring on the authored position even while preview time elapses.</summary>
        [Fact]
        public void RadiusRingCenterStaysPutForNonMovingGrab()
        {
            Core.Geometry.Vec2 center = RadiusRingCenter(Obj("0,0", "0"), 5.0);

            Assert.Equal(0, center.X, precision: 6);
            Assert.Equal(0, center.Y, precision: 6);
        }

        private static Core.Geometry.Vec2 RadiusRingCenter(LevelObject obj, double? previewSeconds)
        {
            System.Type grabRenderer = typeof(LevelCanvas).Assembly.GetType("CtrDxEditor.Rendering.GrabRenderer")!;
            System.Reflection.MethodInfo method = grabRenderer.GetMethod(
                "RadiusRingCenter",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
            return (Core.Geometry.Vec2)method.Invoke(null, [obj, previewSeconds])!;
        }

        private static LevelObject Obj(string path, string moveSpeed, bool hidePath = false)
        {
            XElement element = new("grab",
                new XAttribute("x", "0"), new XAttribute("y", "0"),
                new XAttribute("path", path), new XAttribute("moveSpeed", moveSpeed));
            if (hidePath)
            {
                element.SetAttributeValue("hidePath", "true");
            }
            return new LevelObject(element);
        }
    }
}

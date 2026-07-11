using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests deterministic moving-grab bee and pollen behavior.</summary>
    public class GrabBeeRendererTests
    {
        /// <summary>Pollen is placed at the DX 44-unit spacing, including both segment endpoints.</summary>
        [Fact]
        public void PollenUsesFortyFourUnitSpacing()
        {
            LevelObject grab = Obj("100,0", "50");

            Core.Geometry.Vec2[] particles = [.. GrabBeeRenderer.PollenPoints(grab)];

            Assert.Contains(particles, p => p.X == 0 && p.Y == 0);
            Assert.Contains(particles, p => p.X == 44 && p.Y == 0);
            Assert.Contains(particles, p => p.X == 88 && p.Y == 0);
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

using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Regression coverage for hitbox geometry ported from the active cuttherope-dx physics model.</summary>
    public class HitboxTests
    {
        /// <summary>Candy collision remains in raw game-object units even though its artwork is scaled to 0.71.</summary>
        [Theory]
        [InlineData(HitboxModel.Desktop, -18, -17.333333333333332, 37.333333333333336, 34.666666666666664)]
        [InlineData(HitboxModel.Phone, -19.333333333333332, -20.666666666666668, 35, 35)]
        public void CandyMatchesRawGameObjectBounds(
            HitboxModel model,
            double x,
            double y,
            double width,
            double height)
        {
            LevelBounds? box = HitboxTable.Compute("candy", 0, 0, scale: 0.71, model);

            AssertBounds(box, x, y, width, height);
        }

        /// <summary>Both split candy elements use GetSplitCandyBoundingBox from the game.</summary>
        [Theory]
        [InlineData("candyL")]
        [InlineData("candyR")]
        public void SplitCandyMatchesGameBounds(string element)
        {
            AssertBounds(
                HitboxTable.Compute(element, 0, 0, scale: 0.71, HitboxModel.Desktop),
                -41 / 3.0,
                -11,
                88 / 3.0,
                76 / 3.0);
        }

        /// <summary>Texture-offset boxes use the game's integer center anchor before world-to-level conversion.</summary>
        [Theory]
        [InlineData("bubble", HitboxModel.Desktop, -77, -77, 152, 152)]
        [InlineData("bubble", HitboxModel.Phone, -125, -125, 171, 171)]
        [InlineData("star", HitboxModel.Desktop, -48, -47, 82, 82)]
        [InlineData("star", HitboxModel.Phone, -52, -51, 90, 90)]
        [InlineData("target", HitboxModel.Desktop, -56, 30, 108, 2)]
        [InlineData("target", HitboxModel.Phone, -50, 10, 75, 3)]
        [InlineData("pump", HitboxModel.Desktop, -80, -80, 175, 175)]
        [InlineData("pump", HitboxModel.Phone, -98, -95, 171, 171)]
        public void BoundingBoxesMatchDxWorldGeometry(
            string element,
            HitboxModel model,
            double worldX,
            double worldY,
            double worldWidth,
            double worldHeight)
        {
            AssertBounds(
                HitboxTable.Compute(element, 0, 0, scale: 0.25, model),
                worldX / 3.0,
                worldY / 3.0,
                worldWidth / 3.0,
                worldHeight / 3.0);
        }

        /// <summary>Spike bands are inflated by the 15-unit candy point tolerance on every side.</summary>
        [Fact]
        public void SpikeBandIncludesCandyTolerance()
        {
            // spike1 desktop band = 212 x 10; +15 tolerance each side => 242 x 40, centered.
            AssertBounds(
                HitboxTable.Compute("spike1", 0, 0, scale: 3, HitboxModel.Desktop),
                -242 / 6.0, -40 / 6.0, 242 / 3.0, 40 / 3.0);
        }

        /// <summary>Static spike collision uses the original physics tables, not repacked atlas widths.</summary>
        [Theory]
        [InlineData("spike1", 212, 204)]
        [InlineData("spike2", 333, 318)]
        [InlineData("spike3", 453, 438)]
        [InlineData("spike4", 566, 543)]
        public void StaticSpikesMatchDesktopAndMobilePhysicsConstants(
            string element,
            double desktopWidth,
            double mobileWidth)
        {
            AssertCenteredStrip(element, toggled: false, HitboxModel.Desktop, desktopWidth, 10, tolerance: 15);
            AssertCenteredStrip(element, toggled: false, HitboxModel.Phone, mobileWidth, 30, tolerance: 15);
        }

        /// <summary>Rotatable spike collision uses the separate rotatable-width tables in the game.</summary>
        [Theory]
        [InlineData("spike1", 202, 204)]
        [InlineData("spike2", 319, 354)]
        [InlineData("spike3", 444, 426)]
        [InlineData("spike4", 559, 534)]
        public void RotatableSpikesMatchDesktopAndMobilePhysicsConstants(
            string element,
            double desktopWidth,
            double mobileWidth)
        {
            AssertCenteredStrip(element, toggled: true, HitboxModel.Desktop, desktopWidth, 10, tolerance: 15);
            AssertCenteredStrip(element, toggled: true, HitboxModel.Phone, mobileWidth, 30, tolerance: 15);
        }

        /// <summary>Electrodes select both width reduction and collision-band height from the active model.</summary>
        [Fact]
        public void ElectroMatchesActivePhysicsConstants()
        {
            AssertCenteredStrip("electro", toggled: false, HitboxModel.Desktop, 433, 10, tolerance: 15);
            AssertCenteredStrip("electro", toggled: false, HitboxModel.Phone, 411, 30, tolerance: 15);
        }

        /// <summary>Bouncer bands are inflated by BouncerCollisionRadius (40 desktop / 60 world mobile).</summary>
        [Fact]
        public void BouncerBandIncludesCollisionRadius()
        {
            // bouncer1 desktop 194 x 10, +40 => 274 x 90.
            AssertBounds(
                HitboxTable.Compute("bouncer1", 0, 0, scale: 3, HitboxModel.Desktop),
                -274 / 6.0, -90 / 6.0, 274 / 3.0, 90 / 3.0);
            // bouncer1 mobile 138 x 30, +60 => 258 x 150.
            AssertBounds(
                HitboxTable.Compute("bouncer1", 0, 0, scale: 3, HitboxModel.Phone),
                -258 / 6.0, -150 / 6.0, 258 / 3.0, 150 / 3.0);
        }

        /// <summary>Bouncers use frozen original-asset widths rather than the repacked JSON atlas widths.</summary>
        [Theory]
        [InlineData("bouncer1", 194, 138)]
        [InlineData("bouncer2", 302, 273)]
        public void BouncersMatchActivePhysicsConstants(
            string element,
            double desktopWidth,
            double mobileWidth)
        {
            AssertCenteredStrip(element, toggled: false, HitboxModel.Desktop, desktopWidth, 10, tolerance: 40);
            AssertCenteredStrip(element, toggled: false, HitboxModel.Phone, mobileWidth, 30, tolerance: 60);
        }

        /// <summary>Rocket catch geometry is raw world-space physics and does not inherit the 0.7 art scale.</summary>
        [Fact]
        public void RocketMatchesDesktopAndMobileCatchSlatConstants()
        {
            AssertBounds(
                HitboxTable.Compute("rocket", 0, 0, scale: 0.7, HitboxModel.Desktop),
                -128.9 / 3.0,
                -4.975 / 3.0,
                71.6,
                8.95 / 3.0);
            AssertBounds(
                HitboxTable.Compute("rocket", 0, 0, scale: 0.7, HitboxModel.Phone),
                -43.3,
                -1.45,
                69.6,
                2.9);
        }

        /// <summary>The magic-hat mouth changes to the smaller WP7 rectangle under mobile physics.</summary>
        [Fact]
        public void SockMatchesActivePhysicsModel()
        {
            AssertBounds(
                HitboxTable.Compute("sock", 0, 0, scale: 0.7, HitboxModel.Desktop),
                -30,
                0,
                140 / 3.0,
                5);
            AssertBounds(
                HitboxTable.Compute("sock", 0, 0, scale: 0.7, HitboxModel.Phone),
                -15,
                0,
                30,
                1);
        }

        /// <summary>Hitbox model selection follows the level's useMobilePhysics flag.</summary>
        [Fact]
        public void ModelSelectionFollowsMobilePhysicsSetting()
        {
            Assert.Equal(HitboxModel.Desktop, HitboxTable.ModelFor(useMobilePhysics: false));
            Assert.Equal(HitboxModel.Phone, HitboxTable.ModelFor(useMobilePhysics: true));
        }

        /// <summary>World-space collision geometry is independent of the object's visual sprite scale.</summary>
        [Fact]
        public void VisualScaleDoesNotChangeCollisionBounds()
        {
            LevelBounds? normal = HitboxTable.Compute("candy", 0, 0, scale: 0.71, HitboxModel.Desktop);
            LevelBounds? arbitrary = HitboxTable.Compute("candy", 0, 0, scale: 9, HitboxModel.Desktop);

            Assert.Equal(normal, arbitrary);
        }

        /// <summary>Object coordinates translate collision bounds without changing their size.</summary>
        [Fact]
        public void ObjectPositionTranslatesBounds()
        {
            AssertBounds(
                HitboxTable.Compute("candy", 100, 200, scale: 0.71, HitboxModel.Desktop),
                82,
                200 - (52 / 3.0),
                112 / 3.0,
                104 / 3.0);
        }

        /// <summary>Objects without a ported collision primitive do not receive an indicator.</summary>
        [Theory]
        [InlineData("grab")]
        [InlineData("gravitySwitch")]
        [InlineData("")]
        public void UnsupportedElementsReturnNull(string element)
        {
            Assert.Null(HitboxTable.Compute(element, 0, 0, scale: 1, HitboxModel.Desktop));
        }

        private static void AssertCenteredStrip(
            string element,
            bool toggled,
            HitboxModel model,
            double worldWidth,
            double worldHeight,
            double tolerance = 0)
        {
            LevelBounds? box;
            if (toggled)
            {
                LevelObject obj = new(new XElement(
                    element,
                    new XAttribute("x", "0"),
                    new XAttribute("y", "0"),
                    new XAttribute("toggled", "0")));
                box = HitboxTable.Compute(obj, scale: 3, model);
            }
            else
            {
                box = HitboxTable.Compute(element, 0, 0, scale: 3, model);
            }

            double inflatedWidth = worldWidth + (2 * tolerance);
            double inflatedHeight = worldHeight + (2 * tolerance);
            AssertBounds(
                box,
                -inflatedWidth / 6.0,
                -inflatedHeight / 6.0,
                inflatedWidth / 3.0,
                inflatedHeight / 3.0);
        }

        private static void AssertBounds(
            LevelBounds? actual,
            double x,
            double y,
            double width,
            double height)
        {
            LevelBounds box = Assert.IsType<LevelBounds>(actual);
            Assert.Equal(x, box.X, precision: 9);
            Assert.Equal(y, box.Y, precision: 9);
            Assert.Equal(width, box.W, precision: 9);
            Assert.Equal(height, box.H, precision: 9);
        }
    }
}

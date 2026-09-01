using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the rocket object descriptor and the descriptor Game grouping label.</summary>
    public class RocketDescriptorTests
    {
        /// <summary>The rocket is an Experiments-era object, so it groups apart from the base-game palette items.</summary>
        [Fact]
        public void RocketIsRegisteredInTheExperimentsGroup()
        {
            ObjectDescriptor? rocket = DescriptorTable.CtrObjects.For("rocket");
            Assert.NotNull(rocket);
            Assert.Equal("Rocket", rocket.DisplayName);
            Assert.Equal(int.MaxValue, rocket.MaxCount);
            Assert.Equal("Cut the Rope: Experiments", rocket.Game);
        }

        /// <summary>Objects that predate the Game label fall back to the base game, so adding the label was not a breaking change.</summary>
        [Fact]
        public void ExistingObjectsDefaultToTheBaseGame()
        {
            Assert.Equal("Cut the Rope", DescriptorTable.CtrObjects.For("pump")!.Game);
        }

        /// <summary>Every launch attribute defaults to the value the game itself uses, so a freshly placed rocket round-trips unchanged.</summary>
        [Fact]
        public void RocketExposesLaunchAttributesWithGameDefaults()
        {
            ObjectDescriptor rocket = DescriptorTable.CtrObjects.For("rocket")!;

            AttributeSpec angle = rocket.Attributes.Single(a => a.Name == "angle");
            Assert.Equal(AttrType.Number, angle.Type);
            Assert.Equal("0", angle.Default);

            AttributeSpec impulse = rocket.Attributes.Single(a => a.Name == "impulse");
            Assert.Equal(AttrType.Number, impulse.Type);
            Assert.Equal("20", impulse.Default);

            AttributeSpec impulseFactor = rocket.Attributes.Single(a => a.Name == "impulseFactor");
            Assert.Equal(AttrType.Number, impulseFactor.Type);
            Assert.Equal("0.6", impulseFactor.Default);

            AttributeSpec time = rocket.Attributes.Single(a => a.Name == "time");
            Assert.Equal(AttrType.Number, time.Type);
            Assert.Equal("-1", time.Default);

            AttributeSpec isRotatable = rocket.Attributes.Single(a => a.Name == "isRotatable");
            Assert.Equal(AttrType.Bool, isRotatable.Type);
            Assert.Equal("false", isRotatable.Default);
        }

        /// <summary>
        /// The game scales an authored impulse into world coordinates only under the Time Travel rocket
        /// model, so a rocket placed in such a level starts from the smaller level-coordinate value.
        /// </summary>
        [Fact]
        public void PlacingARocketInATimeTravelLevelDefaultsToTheSmallerImpulse()
        {
            LevelObject rocket = Place(Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\""));

            Assert.Equal("5", rocket.GetAttr("impulse"));
            Assert.Equal("0.6", rocket.GetAttr("impulseFactor"));
        }

        /// <summary>Without the flag the rocket keeps the world-tuned desktop Experiments default.</summary>
        [Fact]
        public void PlacingARocketWithoutTimeTravelPhysicsKeepsTheDescriptorImpulse()
        {
            Assert.Equal("20", Place(Doc("useMobilePhysics=\"true\"")).GetAttr("impulse"));
            Assert.Equal("20", Place(Doc("")).GetAttr("impulse"));
        }

        /// <summary>The flag is a mode of mobile physics, so on its own it does not move the default.</summary>
        [Fact]
        public void TheTimeTravelImpulseNeedsMobilePhysicsToo()
        {
            Assert.Equal("20", Place(Doc("useTimeTravelRocketPhysics=\"true\"")).GetAttr("impulse"));
        }

        /// <summary>Callers with no level context still get the descriptor default.</summary>
        [Fact]
        public void PlacingARocketWithoutADocumentKeepsTheDescriptorImpulse()
        {
            Assert.Equal("20", Place(null).GetAttr("impulse"));
        }

        private static LevelObject Place(LevelDocument? doc)
        {
            return Placement.CreateObject(DescriptorTable.CtrObjects.For("rocket")!, 10, 20, doc);
        }

        private static LevelDocument Doc(string designAttributes)
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign {designAttributes} />
                    </layer>
                    <layer name="Objects" />
                </map>
                """);
        }
    }
}

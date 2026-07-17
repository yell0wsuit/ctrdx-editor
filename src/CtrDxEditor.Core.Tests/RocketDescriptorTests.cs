using System.Linq;

using CtrDxEditor.Core.Descriptors;

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
            Assert.Equal("0", impulse.Default);

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
    }
}

using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for game-accurate electro on/off timing.</summary>
    public class ElectroAnimationTests
    {
        private static LevelObject Electro(string initialDelay, string offTime, string onTime)
        {
            return new LevelObject(new XElement(
                "electro",
                new XAttribute("initialDelay", initialDelay),
                new XAttribute("offTime", offTime),
                new XAttribute("onTime", onTime)));
        }

        /// <summary>Electro starts off, switches on after offTime + initialDelay, then cycles on/off.</summary>
        [Theory]
        [InlineData(0.0, false, "electro_off")]
        [InlineData(1.99, false, "electro_off")]
        [InlineData(2.00, true, "electro_on_1")]
        [InlineData(2.05, true, "electro_on_2")]
        [InlineData(2.10, true, "electro_on_3")]
        [InlineData(2.15, true, "electro_on_4")]
        [InlineData(2.20, true, "electro_on_1")]
        [InlineData(2.99, true, "electro_on_4")]
        [InlineData(3.00, false, "electro_off")]
        [InlineData(4.99, false, "electro_off")]
        [InlineData(5.00, true, "electro_on_1")]
        public void SpriteKeyFollowsOffThenOnCycle(double elapsedSeconds, bool expectedOn, string expectedKey)
        {
            LevelObject electro = Electro(initialDelay: "0.0", offTime: "2.0", onTime: "1.0");

            Assert.Equal(expectedOn, ElectroAnimation.IsOn(electro, elapsedSeconds));
            Assert.Equal(expectedKey, ElectroAnimation.SpriteKey(electro, elapsedSeconds));
        }

        /// <summary>Negative initial delay offsets the first off phase like LoadSpikes does in cuttherope-dx.</summary>
        [Fact]
        public void NegativeInitialDelayShortensFirstOffPhase()
        {
            LevelObject electro = Electro(initialDelay: "-1.6", offTime: "2.4", onTime: "0.8");

            Assert.False(ElectroAnimation.IsOn(electro, 0.79));
            Assert.True(ElectroAnimation.IsOn(electro, 0.80));
        }

        /// <summary>Only a positive on duration supplies active electro preview timing.</summary>
        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData("-1", false)]
        [InlineData("invalid", false)]
        [InlineData(null, false)]
        public void ActiveTimingRequiresPositiveOnTime(string? onTime, bool expected)
        {
            XElement element = new("electro");
            if (onTime is not null)
            {
                element.SetAttributeValue("onTime", onTime);
            }

            Assert.Equal(expected, ElectroAnimation.HasActiveTiming(new LevelObject(element)));
        }

        /// <summary>Without preview playback, electro renders the lit editor preview frame.</summary>
        [Fact]
        public void NullElapsedUsesEditorPreviewSpriteKey()
        {
            Assert.Equal("electro", ElectroAnimation.SpriteKey(Electro("0", "2", "1"), null));
        }
    }
}

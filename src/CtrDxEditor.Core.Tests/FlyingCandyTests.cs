using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests Time Travel's flying candy flag against the game's parse and leader rules.</summary>
    public class FlyingCandyTests
    {
        /// <summary>isDriven parses like the game's GetBoolAttribute: "true" in any case, or "1".</summary>
        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("yes", false)]
        [InlineData(null, false)]
        public void IsDrivenParsesLikeTheGame(string? value, bool expected)
        {
            Assert.Equal(expected, FlyingCandy.IsDriven(Candy(value)));
        }

        /// <summary>Only a plain candy can fly; the game reads isDriven on &lt;candy&gt; alone.</summary>
        [Fact]
        public void OnlyPlainCandyIsDriven()
        {
            LevelObject half = new(new XElement("candyL", new XAttribute("isDriven", "true")));
            Assert.False(FlyingCandy.IsDriven(half));
        }

        /// <summary>A flying candy with a plain candy to follow grows wings.</summary>
        [Fact]
        public void FlyingCandyWithLeaderHasWings()
        {
            LevelObject flier = Candy("true");
            Assert.True(FlyingCandy.HasWings(flier, [Candy(null), flier], twoParts: false));
        }

        /// <summary>A split candy pair leads only in a twoParts level, where the game builds the split candy.</summary>
        [Fact]
        public void SplitCandyIsALeaderOnlyInTwoPartsLevel()
        {
            LevelObject flier = Candy("true");
            LevelObject left = new(new XElement("candyL"));
            LevelObject right = new(new XElement("candyR"));
            Assert.True(FlyingCandy.HasWings(flier, [left, right, flier], twoParts: true));
            Assert.False(FlyingCandy.HasWings(flier, [left, right, flier], twoParts: false));
        }

        /// <summary>With nothing to follow the game plays it as an ordinary candy, so no wings.</summary>
        [Fact]
        public void FlyingCandyWithoutLeaderHasNoWings()
        {
            LevelObject a = Candy("true");
            LevelObject b = Candy("true");
            Assert.False(FlyingCandy.HasWings(a, [a], twoParts: false));
            Assert.False(FlyingCandy.HasWings(a, [a, b], twoParts: false));
            Assert.False(FlyingCandy.HasWings(a, [a, new LevelObject(new XElement("candyL"))], twoParts: true));
        }

        /// <summary>A plain candy never has wings.</summary>
        [Fact]
        public void PlainCandyHasNoWings()
        {
            LevelObject plain = Candy(null);
            Assert.False(FlyingCandy.HasWings(plain, [plain, Candy(null)], twoParts: false));
        }

        /// <summary>Static rendering shows the first flap frame; preview loops quads 0-3 every 0.02 s.</summary>
        [Theory]
        [InlineData(null, 0)]
        [InlineData(0.0, 0)]
        [InlineData(0.021, 1)]
        [InlineData(0.061, 3)]
        [InlineData(0.081, 0)]
        public void FlapFrameLoopsFourQuads(double? seconds, int expected)
        {
            Assert.Equal(expected, FlyingCandy.FlapFrame(seconds));
        }

        private static LevelObject Candy(string? isDriven)
        {
            XElement e = new("candy", new XAttribute("x", 0), new XAttribute("y", 0));
            if (isDriven is not null)
            {
                e.SetAttributeValue("isDriven", isDriven);
            }
            return new LevelObject(e);
        }
    }
}

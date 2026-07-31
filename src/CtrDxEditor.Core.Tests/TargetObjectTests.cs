using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests targetType handling against the game's skin resolution rules.</summary>
    public class TargetObjectTests
    {
        private static LevelObject Target(string? targetType = null)
        {
            XElement element = new("target");
            if (targetType is not null)
            {
                element.SetAttributeValue("targetType", targetType);
            }

            return new LevelObject(element);
        }

        /// <summary>Values inside the skin range are the level's own skin choice.</summary>
        [Theory]
        [InlineData("1")]
        [InlineData("8")]
        [InlineData("16")]
        public void SkinReportsResolvableValues(string targetType)
        {
            Assert.Equal(targetType, TargetObject.Skin(Target(targetType)));
        }

        /// <summary>
        /// Everything the game would not resolve to a skin - absent, zero, negative, past the last skin, or
        /// not a number - reads back as the player's own choice, the same fallback the game applies.
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-3")]
        [InlineData("17")]
        [InlineData("banana")]
        [InlineData("")]
        public void SkinFallsBackToPlayerChoice(string? targetType)
        {
            Assert.Equal(TargetObject.PlayerChoice, TargetObject.Skin(Target(targetType)));
        }

        /// <summary>Choosing a skin writes it; choosing the player's own removes the attribute.</summary>
        [Fact]
        public void SetSkinWritesAndClears()
        {
            LevelObject target = Target();

            TargetObject.SetSkin(target, "5");
            Assert.Equal("5", target.GetAttr("targetType"));

            TargetObject.SetSkin(target, TargetObject.PlayerChoice);
            Assert.Null(target.GetAttr("targetType"));
        }

        /// <summary>Out-of-range writes are ignored, so the panel can never author a skin the game drops.</summary>
        [Theory]
        [InlineData("17")]
        [InlineData("-1")]
        [InlineData("nope")]
        public void SetSkinIgnoresUnresolvableValues(string skin)
        {
            LevelObject target = Target("6");

            TargetObject.SetSkin(target, skin);

            Assert.Equal("6", target.GetAttr("targetType"));
        }

        /// <summary>Only the target element carries a skin.</summary>
        [Fact]
        public void IsTargetMatchesOnlyTheTargetElement()
        {
            Assert.True(TargetObject.IsTarget("target"));
            Assert.False(TargetObject.IsTarget("candy"));
        }
    }
}

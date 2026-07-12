using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests inner-candy frame resolution for the lantern, matching the game's skin rule.</summary>
    public class LanternInnerCandyTests
    {
        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 4)]
        [InlineData(2, 5)]
        public void Skins0To2UseLanternAtlasQuads3To5(int skin, int expectedQuad)
        {
            LanternInnerCandyFrame frame = LanternInnerCandy.Resolve(skin);
            Assert.Equal("images/obj_lantern", frame.AtlasImageBase);
            Assert.Equal("images/obj_lantern.json", frame.AtlasJsonPath);
            Assert.Equal(expectedQuad, frame.Quad);
        }

        [Fact]
        public void Skin3UsesItsCandyAtlasQuad10()
        {
            LanternInnerCandyFrame frame = LanternInnerCandy.Resolve(3);
            Assert.Equal("images/candies/obj_candy_04", frame.AtlasImageBase);
            Assert.Equal("images/candies/obj_candy_04.json", frame.AtlasJsonPath);
            Assert.Equal(10, frame.Quad);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(999)]
        public void OutOfRangeSkinFallsBackToSkin0(int skin)
        {
            LanternInnerCandyFrame frame = LanternInnerCandy.Resolve(skin);
            Assert.Equal("images/obj_lantern", frame.AtlasImageBase);
            Assert.Equal(3, frame.Quad);
        }
    }
}

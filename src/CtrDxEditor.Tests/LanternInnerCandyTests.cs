using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests inner-candy frame resolution for the lantern, matching the game's skin rule.</summary>
    public class LanternInnerCandyTests
    {
        /// <summary>Built-in candy skins use the dedicated frames baked into the lantern atlas.</summary>
        /// <param name="skin">Candy skin index.</param>
        /// <param name="expectedQuad">Expected lantern-atlas quad.</param>
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

        /// <summary>The fourth skin resolves its lantern frame from its own candy atlas.</summary>
        [Fact]
        public void Skin3UsesItsCandyAtlasQuad10()
        {
            LanternInnerCandyFrame frame = LanternInnerCandy.Resolve(3);
            Assert.Equal("images/candies/obj_candy_04", frame.AtlasImageBase);
            Assert.Equal("images/candies/obj_candy_04.json", frame.AtlasJsonPath);
            Assert.Equal(10, frame.Quad);
        }

        /// <summary>Invalid skin indices fall back to the default lantern candy frame.</summary>
        /// <param name="skin">Out-of-range candy skin index.</param>
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

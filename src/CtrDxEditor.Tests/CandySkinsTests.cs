using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the candy skin catalog's index-to-resource mapping, mirroring the game's helper.</summary>
    public class CandySkinsTests
    {
        /// <summary>Skin 0 is the default "01_new" atlas; 1..51 map to obj_candy_02..obj_candy_52.</summary>
        [Theory]
        [InlineData(0, "images/candies/obj_candy_01_new")]
        [InlineData(1, "images/candies/obj_candy_02")]
        [InlineData(51, "images/candies/obj_candy_52")]
        public void ResourceBaseMapsSkinIndexToAtlas(int skin, string expected)
        {
            Assert.Equal(expected, CandySkins.ResourceBase(skin));
        }

        /// <summary>Out-of-range indices fall back to the default skin, matching CandySkinHelper.</summary>
        [Theory]
        [InlineData(-1)]
        [InlineData(52)]
        [InlineData(999)]
        public void ResourceBaseFallsBackToDefaultForOutOfRange(int skin)
        {
            Assert.Equal("images/candies/obj_candy_01_new", CandySkins.ResourceBase(skin));
        }

        /// <summary>JsonPath is the resource base with a .json suffix.</summary>
        [Fact]
        public void JsonPathAppendsJsonSuffix()
        {
            Assert.Equal("images/candies/obj_candy_02.json", CandySkins.JsonPath(1));
        }
    }
}

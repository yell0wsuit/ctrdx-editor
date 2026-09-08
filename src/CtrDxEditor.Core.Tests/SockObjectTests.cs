using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests DX magic-hat visual state selection.</summary>
    public class SockObjectTests
    {
        /// <summary>Verifies season and transporter group select the same atlas and quad as LoadSock.</summary>
        [Theory]
        [InlineData(null, false, "sock")]
        [InlineData("0", false, "sock")]
        [InlineData("1", false, "sock_grouped")]
        [InlineData("invalid", false, "sock")]
        [InlineData("0", true, "sock_xmas")]
        [InlineData("1", true, "sock_xmas_grouped")]
        public void SpriteKeyUsesChristmasAtlasAndGroupQuad(string? group, bool isXmas, string expected)
        {
            Assert.Equal(expected, SockObject.SpriteKey(Sock(group), isXmas));
        }

        /// <summary>
        /// A group past the two the art bakes a color for wears a generated band, and wraps back through
        /// the generated colors once they run out - the same repeat as the game's ColorForGroup.
        /// </summary>
        [Theory]
        [InlineData("2", "sock_band_2")]
        [InlineData("3", "sock_band_3")]
        [InlineData("4", "sock_band_4")]
        [InlineData("5", "sock_band_5")]
        [InlineData("6", "sock_band_2")]
        [InlineData("7", "sock_band_3")]
        [InlineData("100", "sock_band_4")]
        public void SpriteKeyGivesGeneratedBandGroupsTheirOwnArt(string group, string expected)
        {
            Assert.Equal(expected, SockObject.SpriteKey(Sock(group), isXmas: false));
        }

        /// <summary>
        /// The Christmas art draws two socks and stops there, so a generated-band group has nothing
        /// seasonal to wear and falls back to the magic hat, matching SockArt.TextureFor.
        /// </summary>
        [Theory]
        [InlineData("2", "sock_band_2")]
        [InlineData("3", "sock_band_3")]
        public void GeneratedBandGroupsHaveNoChristmasVariant(string group, string expected)
        {
            Assert.Equal(expected, SockObject.SpriteKey(Sock(group), isXmas: true));
        }

        /// <summary>
        /// A negative group is data the level authored, not a slot: SockArt clamps it to zero before it
        /// reaches the art, so it draws the plain hat rather than indexing off the front of the palette.
        /// </summary>
        [Theory]
        [InlineData("-1", false, "sock")]
        [InlineData("-2", false, "sock")]
        [InlineData("-2", true, "sock_xmas")]
        public void NegativeGroupsDrawThePlainHat(string group, bool isXmas, string expected)
        {
            Assert.Equal(expected, SockObject.SpriteKey(Sock(group), isXmas));
        }

        private static LevelObject Sock(string? group)
        {
            XElement element = new("sock");
            if (group is not null)
            {
                element.SetAttributeValue("group", group);
            }
            return new LevelObject(element);
        }

        /// <summary>A lone grouped pair needs no number: plain vs grouped is already distinct.</summary>
        [Fact]
        public void SingleNonzeroGroupIsUnlabeled()
        {
            List<LevelObject> objects = [Sock("0"), Sock("0"), Sock("1"), Sock("1")];

            Assert.Null(SockObject.GroupLabel(objects[2], objects));
        }

        /// <summary>Group zero hats are never labeled.</summary>
        [Fact]
        public void GroupZeroIsUnlabeled()
        {
            List<LevelObject> objects = [Sock("0"), Sock("1"), Sock("2")];

            Assert.Null(SockObject.GroupLabel(objects[0], objects));
        }

        /// <summary>Two distinct nonzero groups label the grouped hats with their group value.</summary>
        [Fact]
        public void TwoNonzeroGroupsLabelGroupedHats()
        {
            List<LevelObject> objects = [Sock("0"), Sock("1"), Sock("2")];

            Assert.Equal("1", SockObject.GroupLabel(objects[1], objects));
            Assert.Equal("2", SockObject.GroupLabel(objects[2], objects));
        }

        /// <summary>The label is the canonical integer value regardless of formatting.</summary>
        [Fact]
        public void LabelNormalizesGroupValue()
        {
            List<LevelObject> objects = [Sock("1"), Sock(" 002 ")];

            Assert.Equal("2", SockObject.GroupLabel(objects[1], objects));
        }
    }
}

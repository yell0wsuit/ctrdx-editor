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
        [InlineData("-2", false, "sock_grouped")]
        [InlineData("invalid", false, "sock")]
        [InlineData("0", true, "sock_xmas")]
        [InlineData("3", true, "sock_xmas_grouped")]
        public void SpriteKeyUsesChristmasAtlasAndGroupQuad(string? group, bool isXmas, string expected)
        {
            XElement element = new("sock");
            if (group is not null)
            {
                element.SetAttributeValue("group", group);
            }

            Assert.Equal(expected, SockObject.SpriteKey(new LevelObject(element), isXmas));
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

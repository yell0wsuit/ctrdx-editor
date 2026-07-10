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
    }
}

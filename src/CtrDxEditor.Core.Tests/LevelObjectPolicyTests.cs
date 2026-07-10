using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests level-aware object default and attribute visibility rules.</summary>
    public class LevelObjectPolicyTests
    {
        /// <summary>Two-part grabs still default their backing part attribute to left on placement.</summary>
        [Fact]
        public void HalfCandyGrabDefaultsPartToLeft()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));
            LevelObject grab = new(new XElement("grab"));

            LevelObjectPolicy.ApplyDefaults(grab, doc);

            Assert.Equal("L", grab.GetAttr("part"));
        }

        /// <summary>Single-candy grabs do not receive a backing part attribute.</summary>
        [Fact]
        public void FullCandyGrabDoesNotDefaultPart()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = new(new XElement("grab"));

            LevelObjectPolicy.ApplyDefaults(grab, doc);

            Assert.Null(grab.GetAttr("part"));
        }

        /// <summary>The raw grab part attribute is hidden because attachTo subsumes it.</summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void GrabPartVisibilityIsAlwaysHidden(bool twoParts, bool visible)
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, twoParts, NightLevel: false));

            Assert.Equal(visible, LevelObjectPolicy.IsAttributeVisible("grab", "part", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("grab", "length", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("star", "timeout", doc));
        }

        /// <summary>The first magic hat placed defaults to group zero.</summary>
        [Fact]
        public void FirstSockDefaultsToGroupZero()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject sock = new(new XElement("sock"));

            LevelObjectPolicy.ApplyDefaults(sock, doc);

            Assert.Equal("0", sock.GetAttr("group"));
        }

        /// <summary>A second hat completes the first hat's pair by reusing its group.</summary>
        [Fact]
        public void SecondSockCompletesFirstPair()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            doc.Add(new LevelObject(new XElement("sock", new XAttribute("group", "0"))));
            LevelObject sock = new(new XElement("sock"));

            LevelObjectPolicy.ApplyDefaults(sock, doc);

            Assert.Equal("0", sock.GetAttr("group"));
        }

        /// <summary>A third hat starts a fresh group once the first pair is complete.</summary>
        [Fact]
        public void ThirdSockStartsNewGroup()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            doc.Add(new LevelObject(new XElement("sock", new XAttribute("group", "0"))));
            doc.Add(new LevelObject(new XElement("sock", new XAttribute("group", "0"))));
            LevelObject sock = new(new XElement("sock"));

            LevelObjectPolicy.ApplyDefaults(sock, doc);

            Assert.Equal("1", sock.GetAttr("group"));
        }
    }
}

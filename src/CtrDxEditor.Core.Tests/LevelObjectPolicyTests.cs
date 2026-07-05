using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests level-aware object default and attribute visibility rules.</summary>
    public class LevelObjectPolicyTests
    {
        [Fact]
        public void HalfCandyGrabDefaultsPartToLeft()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));
            LevelObject grab = new(new XElement("grab"));

            LevelObjectPolicy.ApplyDefaults(grab, doc);

            Assert.Equal("L", grab.GetAttr("part"));
        }

        [Fact]
        public void FullCandyGrabDoesNotDefaultPart()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = new(new XElement("grab"));

            LevelObjectPolicy.ApplyDefaults(grab, doc);

            Assert.Null(grab.GetAttr("part"));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void GrabPartVisibilityFollowsHalfCandyMode(bool twoParts, bool visible)
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, twoParts, NightLevel: false));

            Assert.Equal(visible, LevelObjectPolicy.IsAttributeVisible("grab", "part", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("grab", "length", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("star", "timeout", doc));
        }
    }
}

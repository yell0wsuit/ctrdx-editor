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

        /// <summary>The first mouse placed is numbered index one (mice activate in index order).</summary>
        [Fact]
        public void FirstGapDefaultsToIndexOne()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject gap = new(new XElement("gap"));

            LevelObjectPolicy.ApplyDefaults(gap, doc);

            Assert.Equal("1", gap.GetAttr("index"));
        }

        /// <summary>The auto-numbered index is hidden from the property panel; the other mouse fields show.</summary>
        [Fact]
        public void GapIndexFieldIsHidden()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            Assert.False(LevelObjectPolicy.IsAttributeVisible("gap", "index", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("gap", "radius", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("gap", "activeTime", doc));
            Assert.True(LevelObjectPolicy.IsAttributeVisible("gap", "angle", doc));
        }

        /// <summary>A new mouse takes one past the highest existing index, not the count.</summary>
        [Fact]
        public void GapIndexTakesMaxExistingPlusOne()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            doc.Add(new LevelObject(new XElement("gap", new XAttribute("index", "1"))));
            doc.Add(new LevelObject(new XElement("gap", new XAttribute("index", "3"))));
            LevelObject gap = new(new XElement("gap"));

            LevelObjectPolicy.ApplyDefaults(gap, doc);

            Assert.Equal("4", gap.GetAttr("index"));
        }

        /// <summary>Decimal x/y coordinates are truncated toward zero, matching the game loader.</summary>
        [Theory]
        [InlineData("12.9", "12")]
        [InlineData("-12.9", "-12")]
        [InlineData("100.0", "100")]
        [InlineData("-0.5", "0")]
        public void DropCoordinateDecimalsTruncatesTowardZero(string raw, string expected)
        {
            LevelDocument doc = ObjectsDoc($"<grab x=\"{raw}\" y=\"{raw}\" />");

            Assert.True(LevelObjectPolicy.DropCoordinateDecimals(doc));
            LevelObject obj = doc.AllObjects[0];
            Assert.Equal(expected, obj.GetAttr("x"));
            Assert.Equal(expected, obj.GetAttr("y"));
        }

        /// <summary>Integer coordinates are left untouched and report no change.</summary>
        [Fact]
        public void DropCoordinateDecimalsLeavesIntegersUnchanged()
        {
            LevelDocument doc = ObjectsDoc("<grab x=\"100\" y=\"-40\" />");

            Assert.False(LevelObjectPolicy.DropCoordinateDecimals(doc));
            LevelObject obj = doc.AllObjects[0];
            Assert.Equal("100", obj.GetAttr("x"));
            Assert.Equal("-40", obj.GetAttr("y"));
        }

        /// <summary>Unparseable and out-of-range coordinates are left verbatim rather than fabricated.</summary>
        [Theory]
        [InlineData("nan")]
        [InlineData("2147483648")]
        public void DropCoordinateDecimalsLeavesInvalidValuesVerbatim(string raw)
        {
            LevelDocument doc = ObjectsDoc($"<grab x=\"{raw}\" y=\"10\" />");

            Assert.False(LevelObjectPolicy.DropCoordinateDecimals(doc));
            Assert.Equal(raw, doc.AllObjects[0].GetAttr("x"));
        }

        /// <summary>gameDesign mapOffsetX/mapOffsetY decimals are truncated too, matching the game.</summary>
        [Fact]
        public void DropCoordinateDecimalsTruncatesMapOffsets()
        {
            LevelDocument doc = LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" />" +
                "<gameDesign mapOffsetX=\"5.7\" mapOffsetY=\"-3.2\" /></layer>" +
                "<layer name=\"Objects\"></layer></map>");

            Assert.True(LevelObjectPolicy.DropCoordinateDecimals(doc));
            Assert.Equal("5", doc.GameDesignElement!.Attribute("mapOffsetX")!.Value);
            Assert.Equal("-3", doc.GameDesignElement!.Attribute("mapOffsetY")!.Value);
        }

        private static LevelDocument ObjectsDoc(string objectsXml)
        {
            return LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>" +
                "<layer name=\"Objects\">" + objectsXml + "</layer></map>");
        }
    }
}

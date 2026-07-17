using System.IO;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for preserving level XML during load/save round trips.</summary>
    public class RoundTripTests
    {
        /// <summary>Verifies that loading and saving fixture levels preserves XML semantics.</summary>
        [Theory]
        [InlineData("TestData/2_21.xml")]
        [InlineData("TestData/5_1.xml")]
        [InlineData("TestData/16_5.xml")] // official level with spinner (path="0,0"+rotateSpeed) and plain bouncers
        [InlineData("TestData/17_2.xml")] // official level with a manual conveyor (transporter)
        public void LoadThenSaveIsSemanticallyIdentical(string path)
        {
            string original = File.ReadAllText(path);
            LevelDocument doc = LevelDocument.Parse(original);

            string saved = doc.Save();

            // Semantic identity: same element/attribute tree, ignoring insignificant whitespace.
            XDocument before = XDocument.Parse(original);
            XDocument after = XDocument.Parse(saved);
            Assert.True(XNode.DeepEquals(Normalize(before), Normalize(after)),
                $"Round-trip changed the document for {path}.");
        }

        /// <summary>Verifies that localized layer data survives round-trip serialization.</summary>
        [Fact]
        public void LocaleLayersSurviveRoundTrip()
        {
            LevelDocument doc = LevelDocument.Parse(File.ReadAllText("TestData/5_1.xml"));
            XDocument after = XDocument.Parse(doc.Save());

            string[] localeLayers = [.. after.Root!.Elements("layer")
                .Select(l => (string?)l.Attribute("name") ?? string.Empty)];
            Assert.Contains("Ru", localeLayers);
            Assert.Contains("Ja", localeLayers);
        }

        /// <summary>Star timeout values survive round-trip as authored, including decimals and zero.</summary>
        [Fact]
        public void StarTimeoutsSurviveRoundTrip()
        {
            const string original =
                "<level width=\"640\" height=\"480\">" +
                "<star x=\"100\" y=\"120\" timeout=\"4.5\" />" +
                "<star x=\"200\" y=\"220\" timeout=\"0\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(original);
            XDocument after = XDocument.Parse(doc.Save());

            string[] timeouts = [.. after.Root!.Elements("star").Select(s => (string?)s.Attribute("timeout") ?? string.Empty)];
            Assert.Equal(["4.5", "0"], timeouts);
        }

        /// <summary>Optional pollen visibility remains absent unless authored and preserves an authored value.</summary>
        [Fact]
        public void GrabHidePathRoundTripsWithoutInjectingDefault()
        {
            const string original =
                "<level width='640' height='480'>" +
                "<grab x='100' y='120' path='100,0' moveSpeed='50' />" +
                "<grab x='200' y='220' path='RC40' moveSpeed='50' hidePath='true' />" +
                "</level>";

            XDocument after = XDocument.Parse(LevelDocument.Parse(original).Save());
            XElement[] grabs = [.. after.Root!.Elements("grab")];

            Assert.Null(grabs[0].Attribute("hidePath"));
            Assert.Equal("true", (string?)grabs[1].Attribute("hidePath"));
        }

        /// <summary>A rocket with launch attributes and a mover path round-trips byte-for-byte, including
        /// the mover attributes the editor never turns into fields.</summary>
        [Fact]
        public void RocketWithPathSurvivesRoundTrip()
        {
            const string original =
                "<level width=\"640\" height=\"480\">" +
                "<rocket x=\"100\" y=\"120\" angle=\"45\" impulse=\"20\" impulseFactor=\"0.6\" " +
                "time=\"-1\" isRotatable=\"true\" path=\"R,80\" moveSpeed=\"60\" rotateSpeed=\"90\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(original);
            XDocument after = XDocument.Parse(doc.Save());

            XElement rocket = after.Root!.Element("rocket")!;
            Assert.Equal("45", (string?)rocket.Attribute("angle"));
            Assert.Equal("20", (string?)rocket.Attribute("impulse"));
            Assert.Equal("R,80", (string?)rocket.Attribute("path"));
            Assert.Equal("60", (string?)rocket.Attribute("moveSpeed"));
            Assert.Equal("90", (string?)rocket.Attribute("rotateSpeed"));
            Assert.True(XNode.DeepEquals(
                Normalize(XDocument.Parse(original)), Normalize(after)));
        }

        /// <summary>The snail keeps its <c>load</c> element name and any mover attributes the editor
        /// never turns into fields.</summary>
        [Fact]
        public void SnailSurvivesRoundTrip()
        {
            const string original =
                "<level width=\"640\" height=\"480\">" +
                "<load x=\"100\" y=\"120\" path=\"R,80\" moveSpeed=\"60\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(original);
            XDocument after = XDocument.Parse(doc.Save());

            XElement snail = after.Root!.Element("load")!;
            Assert.Equal("100", (string?)snail.Attribute("x"));
            Assert.Equal("R,80", (string?)snail.Attribute("path"));
            Assert.True(XNode.DeepEquals(
                Normalize(XDocument.Parse(original)), Normalize(after)));
        }

        // Re-serialize with normalized whitespace so DeepEquals compares structure, not formatting.
        private static XDocument Normalize(XDocument d)
        {
            return XDocument.Parse(d.ToString(SaveOptions.None));
        }
    }
}

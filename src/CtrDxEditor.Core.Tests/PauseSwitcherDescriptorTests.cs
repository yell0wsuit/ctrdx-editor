using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the pause switcher: the game reads nothing from it but its position, so the editor must
    /// neither require nor invent anything else.
    /// </summary>
    public class PauseSwitcherDescriptorTests
    {
        private static ObjectDescriptor For(string element)
        {
            ObjectDescriptor? descriptor = DescriptorTable.CtrObjects.For(element);
            Assert.NotNull(descriptor);
            return descriptor;
        }

        /// <summary>The pause switcher is a Time Travel object with nothing to configure.</summary>
        [Fact]
        public void PauseSwitcherDescriptorHasNoAttributes()
        {
            ObjectDescriptor switcher = For("pauseSwitcher");

            Assert.Equal("Cut the Rope: Time Travel", switcher.Game);
            Assert.Equal(int.MaxValue, switcher.MaxCount);
            Assert.Empty(switcher.Attributes);
        }

        /// <summary>A pause switcher is placed as-is; the game reads nothing but its position.</summary>
        [Fact]
        public void PlacingAPauseSwitcherAddsNoAttributes()
        {
            LevelObject switcher = Place(NewLevel(), "pauseSwitcher");

            Assert.Empty(switcher.Element.Attributes());
        }

        /// <summary>A hand-written level with a pause switcher round-trips unchanged.</summary>
        [Fact]
        public void PauseSwitcherSurvivesARoundTrip()
        {
            string xml = """
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects"><candy x="178" y="178" /><target x="300" y="400" /><pauseSwitcher x="60" y="60" /></layer>
                </map>
                """;

            LevelDocument doc = LevelDocument.Parse(xml);

            Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(doc.Save())));
        }

        private static LevelDocument NewLevel(string objects = "")
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects">{objects}</layer>
                </map>
                """);
        }

        private static LevelObject Place(LevelDocument doc, string element)
        {
            LevelObject obj = new(new XElement(element));
            LevelObjectPolicy.ApplyDefaults(obj, doc);
            doc.Add(obj, ObjectLayer(doc));
            return obj;
        }

        private static LevelLayer ObjectLayer(LevelDocument doc)
        {
            return doc.Layers.Single(l => l.Name == "Objects");
        }
    }
}

using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for mutating object layers and their contents.</summary>
    public class LevelLayerMutationTests
    {
        private static LevelDocument TwoLayers()
        {
            return LevelDocument.Parse("""
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings"><map width="320" height="480" /></layer>
                <layer name="a"><candy x="1" y="2" /></layer>
                <layer name="b"><star x="3" y="4" timeout="-1" /></layer>
            </map>
            """);
        }

        /// <summary>Verifies that a new empty layer is appended.</summary>
        [Fact]
        public void AddLayerAppendsEmptyLayer()
        {
            LevelDocument doc = TwoLayers();
            LevelLayer added = doc.AddLayer("c");

            Assert.Equal("c", added.Name);
            Assert.Equal(["a", "b", "c"], doc.Layers.Select(l => l.Name));
            Assert.Empty(added.Objects);
        }

        /// <summary>Verifies that removing a layer also removes its objects.</summary>
        [Fact]
        public void RemoveLayerDropsLayerAndObjects()
        {
            LevelDocument doc = TwoLayers();
            doc.RemoveLayer(doc.Layers[0]);

            Assert.Equal(["b"], doc.Layers.Select(l => l.Name));
            _ = Assert.Single(doc.AllObjects);
        }

        /// <summary>Verifies that an object layer can move relative to its peers.</summary>
        [Fact]
        public void MoveLayerReordersWithinObjectLayers()
        {
            LevelDocument doc = TwoLayers();
            doc.MoveLayer(doc.Layers[1], -1);

            Assert.Equal(["b", "a"], doc.Layers.Select(l => l.Name));
        }

        /// <summary>Verifies that layer movement is clamped at the collection edges.</summary>
        [Fact]
        public void MoveLayerClampsAtEdges()
        {
            LevelDocument doc = TwoLayers();
            doc.MoveLayer(doc.Layers[0], -5);

            Assert.Equal(["a", "b"], doc.Layers.Select(l => l.Name));
        }

        /// <summary>Verifies that an object is appended to the requested target layer.</summary>
        [Fact]
        public void AddPlacesObjectIntoTargetLayer()
        {
            LevelDocument doc = TwoLayers();
            LevelObject obj = new(new XElement("bubble", new XAttribute("x", "5"), new XAttribute("y", "6")));
            doc.Add(obj, doc.Layers[1]);

            Assert.Equal(2, doc.Layers[1].Objects.Count);
            Assert.Equal("bubble", doc.Layers[1].Objects[1].Type);
        }

        /// <summary>Verifies that moving an object reparents it into the target layer.</summary>
        [Fact]
        public void MoveObjectReparentsToTargetLayer()
        {
            LevelDocument doc = TwoLayers();
            LevelObject candy = doc.Layers[0].Objects[0];
            doc.MoveObject(candy, doc.Layers[1]);

            Assert.Empty(doc.Layers[0].Objects);
            Assert.Equal(2, doc.Layers[1].Objects.Count);
        }

        /// <summary>Verifies that candidate layer names must be nonblank and unique.</summary>
        [Fact]
        public void IsLayerNameAvailableRejectsBlankAndDuplicate()
        {
            LevelDocument doc = TwoLayers();

            Assert.False(doc.IsLayerNameAvailable(""));
            Assert.False(doc.IsLayerNameAvailable("a"));
            Assert.True(doc.IsLayerNameAvailable("a", excluding: doc.Layers[0]));
            Assert.True(doc.IsLayerNameAvailable("c"));
        }

        /// <summary>Whitespace around a candidate cannot disguise an existing layer name.</summary>
        [Fact]
        public void IsLayerNameAvailableChecksTrimmedDuplicate()
        {
            LevelDocument doc = TwoLayers();

            Assert.False(doc.IsLayerNameAvailable("  a  "));
        }

        /// <summary>Characters forbidden by XML 1.0 are rejected before serialization.</summary>
        [Fact]
        public void IsLayerNameAvailableRejectsInvalidXmlCharacters()
        {
            LevelDocument doc = TwoLayers();

            Assert.False(doc.IsLayerNameAvailable("bad\u0001name"));
        }

        /// <summary>XML metacharacters remain valid because LINQ to XML escapes attribute values.</summary>
        [Fact]
        public void IsLayerNameAvailableAllowsXmlMetacharacters()
        {
            LevelDocument doc = TwoLayers();

            Assert.True(doc.IsLayerNameAvailable("rock & <roll> \"mix\""));
        }
    }
}

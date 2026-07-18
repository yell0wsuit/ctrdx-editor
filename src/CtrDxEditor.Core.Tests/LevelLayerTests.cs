using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for reading object layers from level documents.</summary>
    public class LevelLayerTests
    {
        private const string MultiLayer = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="320" height="480" />
                <gameDesign ropePhysicsSpeed="1" />
            </layer>
            <layer name="section_a">
                <candy x="10" y="20" />
                <star x="30" y="40" timeout="-1" />
            </layer>
            <layer name="lightbulbs">
                <lightBulb x="1" y="2" bulbNumber="1" />
            </layer>
        </map>
        """;

        /// <summary>Verifies that the settings layer is excluded without disturbing object-layer order.</summary>
        [Fact]
        public void LayersExcludeSettingsAndKeepOrder()
        {
            LevelDocument doc = LevelDocument.Parse(MultiLayer);

            Assert.Equal(2, doc.Layers.Count);
            Assert.Equal("section_a", doc.Layers[0].Name);
            Assert.Equal("lightbulbs", doc.Layers[1].Name);
        }

        /// <summary>Verifies that a layer exposes its direct child objects in XML order.</summary>
        [Fact]
        public void LayerExposesItsObjectsInOrder()
        {
            LevelDocument doc = LevelDocument.Parse(MultiLayer);

            LevelLayer sectionA = doc.Layers[0];
            Assert.Equal(2, sectionA.Objects.Count);
            Assert.Equal("candy", sectionA.Objects[0].Type);
            Assert.Equal("star", sectionA.Objects[1].Type);
        }

        /// <summary>Verifies that renaming a layer updates its backing XML.</summary>
        [Fact]
        public void RenameWritesLayerNameAttribute()
        {
            LevelDocument doc = LevelDocument.Parse(MultiLayer);

            doc.Layers[0].Rename("renamed");

            Assert.Equal("renamed", doc.Layers[0].Name);
            Assert.Contains("name=\"renamed\"", doc.Save());
        }

        /// <summary>Verifies that the aggregate object view flattens every object layer in document order.</summary>
        [Fact]
        public void AllObjectsFlattensEveryLayerInOrder()
        {
            LevelDocument doc = LevelDocument.Parse(MultiLayer);

            Assert.Equal(3, doc.AllObjects.Count);
            Assert.Equal("candy", doc.AllObjects[0].Type);
            Assert.Equal("star", doc.AllObjects[1].Type);
            Assert.Equal("lightBulb", doc.AllObjects[2].Type);
        }
    }
}

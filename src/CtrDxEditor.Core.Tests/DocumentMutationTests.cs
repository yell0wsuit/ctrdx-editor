using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for adding and removing objects in level documents.</summary>
    public class DocumentMutationTests
    {
        private const string Level = """
    <?xml version='1.0' encoding='utf-8'?>
    <map>
        <layer name="settings">
            <map gridSize="32" width="640" height="480" />
            <gameDesign ropePhysicsSpeed="1.0" special="1" twoParts="false" />
        </layer>
        <layer name="Objects">
            <candy x="100" y="100" />
        </layer>
    </map>
    """;

        /// <summary>Verifies that adding then removing an object restores the original object list.</summary>
        [Fact]
        public void AddThenRemoveReturnsToOriginalCount()
        {
            LevelDocument doc = LevelDocument.Parse(Level);
            _ = Assert.Single(doc.Objects);

            LevelObject star = Placement.CreateObject(DescriptorTable.CtrObjects.For("star")!, 50, 60);
            doc.Add(star);
            Assert.Equal(2, doc.Objects.Count);

            LevelDocument.Remove(star);
            _ = Assert.Single(doc.Objects);
            Assert.Equal("candy", doc.Objects[0].Type);
        }

        /// <summary>Verifies that added objects are included in saved XML.</summary>
        [Fact]
        public void AddedObjectAppearsInSavedXml()
        {
            LevelDocument doc = LevelDocument.Parse(Level);
            doc.Add(Placement.CreateObject(DescriptorTable.CtrObjects.For("star")!, 50, 60));

            Assert.Contains("<star", doc.Save());
        }
    }
}

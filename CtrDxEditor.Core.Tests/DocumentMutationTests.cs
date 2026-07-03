using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
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

        [Fact]
        public void Add_then_remove_returns_to_original_count()
        {
            LevelDocument doc = LevelDocument.Parse(Level);
            _ = Assert.Single(doc.Objects);

            LevelObject star = Placement.CreateObject(DescriptorTable.Default.For("star")!, 50, 60);
            doc.Add(star);
            Assert.Equal(2, doc.Objects.Count);

            doc.Remove(star);
            _ = Assert.Single(doc.Objects);
            Assert.Equal("candy", doc.Objects[0].Type);
        }

        [Fact]
        public void Added_object_appears_in_saved_xml()
        {
            LevelDocument doc = LevelDocument.Parse(Level);
            doc.Add(Placement.CreateObject(DescriptorTable.Default.For("star")!, 50, 60));

            Assert.Contains("<star", doc.Save());
        }
    }
}

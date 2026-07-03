using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class LevelDocumentTests
    {
        private const string TwoPartLevel = """
    <?xml version='1.0' encoding='utf-8'?>
    <map>
        <layer name="settings">
            <map gridSize="32" width="320" height="480" />
            <gameDesign ropePhysicsSpeed="1" special="1" twoParts="true" />
        </layer>
        <layer name="Objects">
            <candyL x="101" y="170" />
            <candyR x="232" y="171" />
            <target x="165" y="428" />
            <star x="187" y="327" timeout="-1" />
        </layer>
    </map>
    """;

        [Fact]
        public void Parse_reads_settings_and_objects()
        {
            LevelDocument doc = LevelDocument.Parse(TwoPartLevel);

            Assert.Equal(32, doc.GridSize);
            Assert.Equal(320, doc.Width);
            Assert.Equal(480, doc.Height);
            Assert.True(doc.TwoParts);
            Assert.Equal(4, doc.Objects.Count);
            Assert.Equal("candyL", doc.Objects[0].Type);
            Assert.Equal(165, doc.Objects[2].X);
        }
    }
}

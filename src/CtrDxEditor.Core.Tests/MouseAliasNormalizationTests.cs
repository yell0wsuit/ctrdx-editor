using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Verifies that the legacy <c>mouse</c> tag is normalized to <c>gap</c> on load. The game loads
    /// both tags identically (GameScene.LoadObjects dispatches <c>gap</c> and <c>mouse</c> to
    /// LoadMouse), so renaming is behavior-preserving and lets the editor treat it as one object.
    /// </summary>
    public class MouseAliasNormalizationTests
    {
        private static LevelDocument Parse(string objectsXml)
        {
            return LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>" +
                "<layer name=\"Objects\">" + objectsXml + "</layer></map>");
        }

        /// <summary>A legacy <c>mouse</c> tag becomes a <c>gap</c>, carrying its attributes across untouched.</summary>
        [Fact]
        public void MouseTagRenamedToGap()
        {
            LevelDocument doc = Parse("<mouse x=\"10\" y=\"20\" radius=\"50\" activeTime=\"1.0\" index=\"1\" />");

            bool changed = LevelObjectPolicy.NormalizeMouseAlias(doc);

            Assert.True(changed);
            Assert.Equal("gap", doc.AllObjects[0].Type);
            Assert.Equal("50", doc.AllObjects[0].GetAttr("radius"));
            Assert.Equal("1", doc.AllObjects[0].GetAttr("index"));
        }

        /// <summary>A tag that is already <c>gap</c> reports no change, so opening a normalized level does not mark it dirty.</summary>
        [Fact]
        public void GapTagLeftUnchanged()
        {
            LevelDocument doc = Parse("<gap x=\"10\" y=\"20\" index=\"1\" />");

            bool changed = LevelObjectPolicy.NormalizeMouseAlias(doc);

            Assert.False(changed);
            Assert.Equal("gap", doc.AllObjects[0].Type);
        }
    }
}

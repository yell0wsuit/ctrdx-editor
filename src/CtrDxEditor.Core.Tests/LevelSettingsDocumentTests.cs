using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests reading gameDesign settings and the settings factory/mutators.</summary>
    public class LevelSettingsDocumentTests
    {
        private const string NightTwoPart = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="320" height="960" />
                <gameDesign ropePhysicsSpeed="1.0" special="3" twoParts="true" nightLevel="true" />
            </layer>
            <layer name="Objects" />
        </map>
        """;

        [Fact]
        public void ReadsGameDesignSettings()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart);

            Assert.Equal(1.0f, doc.RopePhysicsSpeed);
            Assert.Equal(3, doc.Special);
            Assert.True(doc.NightLevel);
            Assert.True(doc.TwoParts);

            LevelSettings s = doc.Settings;
            Assert.Equal(320, s.Width);
            Assert.Equal(960, s.Height);
            Assert.Equal(3, s.Special);
            Assert.True(s.TwoParts);
            Assert.True(s.NightLevel);
        }

        [Fact]
        public void DefaultsWhenGameDesignMissing()
        {
            LevelDocument doc = LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"640\" height=\"480\" /></layer></map>");

            Assert.Equal(1.0f, doc.RopePhysicsSpeed);
            Assert.Equal(0, doc.Special);
            Assert.False(doc.NightLevel);
        }

        [Fact]
        public void CreateNewProducesSettingsAndEmptyObjectsLayer()
        {
            LevelSettings s = new(640, 480, 1.0f, 0, TwoParts: true, NightLevel: true);

            LevelDocument doc = LevelDocument.CreateNew(s);

            Assert.Equal(32, doc.GridSize);
            Assert.Equal(640, doc.Width);
            Assert.Equal(480, doc.Height);
            Assert.True(doc.TwoParts);
            Assert.True(doc.NightLevel);
            Assert.Empty(doc.Objects);
            Assert.NotNull(doc.ObjectsLayer);

            // Bools are serialized lowercase to match real maps.
            Assert.Contains("twoParts=\"true\"", doc.Save());
            Assert.Contains("nightLevel=\"true\"", doc.Save());
        }

        [Fact]
        public void UpdateSettingsChangesResolutionAndSpecialButNotLockedFlags()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart); // twoParts=true, nightLevel=true

            doc.UpdateSettings(new LevelSettings(640, 480, 2.0f, 5, TwoParts: false, NightLevel: false));

            Assert.Equal(640, doc.Width);
            Assert.Equal(480, doc.Height);
            Assert.Equal(2.0f, doc.RopePhysicsSpeed);
            Assert.Equal(5, doc.Special);
            // Locked flags ignore the record's values and keep the document's originals.
            Assert.True(doc.TwoParts);
            Assert.True(doc.NightLevel);
        }
    }
}

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
    }
}

using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>The editor no longer authors gameDesign special, but never destroys an authored one.</summary>
    public class SpecialRetirementTests
    {
        private const string LevelWithSpecial = """
            <map>
                <layer name="settings">
                    <map gridSize="32" width="320" height="480" />
                    <gameDesign ropePhysicsSpeed="1.0" special="3" twoParts="false" nightLevel="false" />
                </layer>
                <layer name="Objects" />
            </map>
            """;

        /// <summary>An authored special survives a settings edit untouched.</summary>
        [Fact]
        public void ExistingSpecialSurvivesASettingsEdit()
        {
            LevelDocument document = LevelDocument.Parse(LevelWithSpecial);

            document.UpdateSettings(document.Settings with { NightLevel = true });

            XElement gameDesign = XDocument.Parse(document.Save()).Descendants("gameDesign").Single();
            Assert.Equal("3", gameDesign.Attribute("special")?.Value);
            Assert.Equal("true", gameDesign.Attribute("nightLevel")?.Value);
        }

        /// <summary>A new level never authors special at all.</summary>
        [Fact]
        public void NewLevelsDoNotAuthorSpecial()
        {
            LevelDocument document = LevelDocument.CreateNew(LevelDocument.Parse(LevelWithSpecial).Settings);

            XElement gameDesign = XDocument.Parse(document.Save()).Descendants("gameDesign").Single();
            Assert.Null(gameDesign.Attribute("special"));
        }
    }
}

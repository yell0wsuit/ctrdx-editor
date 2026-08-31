using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the level's Time Travel rocket physics flag, which the editor offers as a mode of the
    /// mobile physics model: it is never read or written on its own.
    /// </summary>
    public class TimeTravelRocketPhysicsSettingTests
    {
        /// <summary>The flag is read when the level also asks for mobile physics.</summary>
        [Fact]
        public void TheFlagIsReadAlongsideMobilePhysics()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"");

            Assert.True(doc.UseMobilePhysics);
            Assert.True(doc.UseTimeTravelRocketPhysics);
            Assert.True(doc.Settings.UseTimeTravelRocketPhysics);
        }

        /// <summary>Without mobile physics the editor does not consider the level to be using it.</summary>
        [Fact]
        public void TheFlagReadsFalseWithoutMobilePhysics()
        {
            Assert.False(Doc("useTimeTravelRocketPhysics=\"true\"").UseTimeTravelRocketPhysics);
        }

        /// <summary>An absent attribute is off, like every other flag.</summary>
        [Fact]
        public void TheFlagDefaultsToOff()
        {
            Assert.False(Doc("useMobilePhysics=\"true\"").UseTimeTravelRocketPhysics);
        }

        /// <summary>Saving writes the attribute beside mobile physics.</summary>
        [Fact]
        public void SavingWritesTheFlag()
        {
            LevelDocument doc = Doc("");
            doc.UpdateSettings(doc.Settings with { UseMobilePhysics = true, UseTimeTravelRocketPhysics = true });

            XElement design = GameDesign(doc);
            Assert.Equal("true", design.Attribute("useMobilePhysics")?.Value);
            Assert.Equal("true", design.Attribute("useTimeTravelRocketPhysics")?.Value);
        }

        /// <summary>It is a mode of mobile physics, so it is never written on its own.</summary>
        [Fact]
        public void SavingNeverWritesTheFlagWithoutMobilePhysics()
        {
            LevelDocument doc = Doc("");
            doc.UpdateSettings(doc.Settings with { UseMobilePhysics = false, UseTimeTravelRocketPhysics = true });

            Assert.Null(GameDesign(doc).Attribute("useTimeTravelRocketPhysics"));
        }

        /// <summary>Turning mobile physics off takes the flag with it, rather than leaving it stranded.</summary>
        [Fact]
        public void TurningOffMobilePhysicsRemovesTheFlag()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"");
            doc.UpdateSettings(doc.Settings with { UseMobilePhysics = false });

            XElement design = GameDesign(doc);
            Assert.Null(design.Attribute("useMobilePhysics"));
            Assert.Null(design.Attribute("useTimeTravelRocketPhysics"));
        }

        /// <summary>A level already using both keeps them across a save that changes nothing else.</summary>
        [Fact]
        public void TheFlagSurvivesASettingsRoundTrip()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"");
            doc.UpdateSettings(doc.Settings);

            Assert.Equal("true", GameDesign(doc).Attribute("useTimeTravelRocketPhysics")?.Value);
        }

        private static XElement GameDesign(LevelDocument doc)
        {
            return XDocument.Parse(doc.Save()).Descendants("gameDesign").Single();
        }

        private static LevelDocument Doc(string designAttributes)
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign {designAttributes} />
                    </layer>
                    <layer name="Objects" />
                </map>
                """);
        }
    }
}

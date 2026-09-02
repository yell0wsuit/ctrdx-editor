using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests reading and writing the gameDesign water attributes.</summary>
    public class WaterDocumentTests
    {
        private const string WaterLevelXml = """
            <map>
              <layer name="settings">
                <map gridSize="32" width="640" height="480" />
                <gameDesign ropePhysicsSpeed="1" special="0" twoParts="false" nightLevel="false" water="120.5" waterSpeed="12" />
              </layer>
              <layer name="Objects" />
            </map>
            """;

        private const string NoWaterXml = """
            <map>
              <layer name="settings">
                <map gridSize="32" width="640" height="480" />
                <gameDesign ropePhysicsSpeed="1" special="0" twoParts="false" nightLevel="false" />
              </layer>
              <layer name="Objects" />
            </map>
            """;

        /// <summary>Water attributes parse with the invariant decimal format the game uses.</summary>
        [Fact]
        public void ReadsWaterAttributes()
        {
            LevelDocument doc = LevelDocument.Parse(WaterLevelXml);

            Assert.Equal(120.5f, doc.Water);
            Assert.Equal(12f, doc.WaterSpeed);
        }

        /// <summary>A level without water reads as zero rather than throwing.</summary>
        [Fact]
        public void MissingWaterAttributesReadAsZero()
        {
            LevelDocument doc = LevelDocument.Parse(NoWaterXml);

            Assert.Equal(0f, doc.Water);
            Assert.Equal(0f, doc.WaterSpeed);
        }

        /// <summary>Water attributes survive a load/save round-trip unchanged.</summary>
        [Fact]
        public void WaterSurvivesRoundTrip()
        {
            LevelDocument doc = LevelDocument.Parse(WaterLevelXml);

            Assert.Contains("water=\"120.5\"", doc.Save());
            Assert.Contains("waterSpeed=\"12\"", doc.Save());
        }

        /// <summary>Settings expose water alongside the other gameDesign values.</summary>
        [Fact]
        public void SettingsCarryWater()
        {
            LevelSettings settings = LevelDocument.Parse(WaterLevelXml).Settings;

            Assert.Equal(120.5f, settings.Water);
            Assert.Equal(12f, settings.WaterSpeed);
        }

        /// <summary>Updating settings writes both attributes with invariant formatting.</summary>
        [Fact]
        public void UpdateSettingsWritesWater()
        {
            LevelDocument doc = LevelDocument.Parse(NoWaterXml);

            doc.UpdateSettings(doc.Settings with { Water = 200f, WaterSpeed = 12.5f });

            Assert.Equal(200f, doc.Water);
            Assert.Equal(12.5f, doc.WaterSpeed);
            Assert.Contains("water=\"200\"", doc.Save());
            Assert.Contains("waterSpeed=\"12.5\"", doc.Save());
        }

        /// <summary>Zeroing water removes the attributes, keeping water-free levels free of noise.</summary>
        [Fact]
        public void UpdateSettingsRemovesZeroedWater()
        {
            LevelDocument doc = LevelDocument.Parse(WaterLevelXml);

            doc.UpdateSettings(doc.Settings with { Water = 0f, WaterSpeed = 0f });

            Assert.DoesNotContain("water=", doc.Save());
            Assert.DoesNotContain("waterSpeed=", doc.Save());
        }

        /// <summary>A new level with no water requested carries no water attributes.</summary>
        [Fact]
        public void CreateNewOmitsWaterByDefault()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));

            Assert.DoesNotContain("water=", doc.Save());
            Assert.Equal(0f, doc.Water);
        }

        /// <summary>A new level can be created with water already set.</summary>
        [Fact]
        public void CreateNewWritesRequestedWater()
        {
            LevelDocument doc = LevelDocument.CreateNew(
                new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false, Water: 150f));

            Assert.Equal(150f, doc.Water);
            Assert.Contains("water=\"150\"", doc.Save());
        }
    }
}

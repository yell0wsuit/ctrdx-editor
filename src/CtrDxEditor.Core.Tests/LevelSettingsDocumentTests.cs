using System.Linq;

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

        /// <summary>The gameDesign element populates both the document properties and the <see cref="LevelSettings"/> snapshot.</summary>
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

        /// <summary>A level with no gameDesign element falls back to defaults instead of failing to parse.</summary>
        [Fact]
        public void DefaultsWhenGameDesignMissing()
        {
            LevelDocument doc = LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"640\" height=\"480\" /></layer></map>");

            Assert.Equal(1.0f, doc.RopePhysicsSpeed);
            Assert.Equal(0, doc.Special);
            Assert.False(doc.NightLevel);
        }

        /// <summary>Settings-layer recognition matches the game's case-insensitive lookup.</summary>
        [Fact]
        public void MixedCaseSettingsLayerSuppliesMetadataAndIsNotAnObjectLayer()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="SeTTings">
                        <map gridSize="16" width="640" height="960" />
                        <gameDesign ropePhysicsSpeed="2" special="4" />
                    </layer>
                    <layer name="Objects"><candy x="1" y="2" /></layer>
                </map>
                """);

            Assert.Equal(16, doc.GridSize);
            Assert.Equal(640, doc.Width);
            Assert.Equal(960, doc.Height);
            Assert.Equal(2f, doc.RopePhysicsSpeed);
            Assert.Equal(4, doc.Special);
            Assert.Equal("Objects", Assert.Single(doc.Layers).Name);
            _ = Assert.Single(doc.AllObjects);
        }

        /// <summary>When malformed XML contains several settings layers, only the first supplies metadata.</summary>
        [Fact]
        public void FirstSettingsLayerIsAuthoritativeRegardlessOfCase()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="Settings"><map width="320" height="480" /></layer>
                    <layer name="SETTINGS"><map width="999" height="999" /></layer>
                    <layer name="Objects" />
                </map>
                """);

            Assert.Equal(320, doc.Width);
            Assert.Equal(480, doc.Height);
            Assert.Equal("Objects", Assert.Single(doc.Layers).Name);
        }

        /// <summary>A new level starts with an empty Objects layer and serializes its bools lowercase, the way real maps store them.</summary>
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
            Assert.Empty(doc.AllObjects);
            Assert.Equal("Objects", Assert.Single(doc.Layers).Name);

            // Bools are serialized lowercase to match real maps.
            Assert.Contains("twoParts=\"true\"", doc.Save());
            Assert.Contains("nightLevel=\"true\"", doc.Save());
        }

        /// <summary>Updating settings rewrites resolution, special, and both flags together.</summary>
        [Fact]
        public void UpdateSettingsChangesResolutionSpecialAndFlags()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart); // twoParts=true, nightLevel=true

            doc.UpdateSettings(new LevelSettings(640, 480, 2.0f, 5, TwoParts: false, NightLevel: false));

            Assert.Equal(640, doc.Width);
            Assert.Equal(480, doc.Height);
            Assert.Equal(2.0f, doc.RopePhysicsSpeed);
            Assert.Equal(5, doc.Special);
            Assert.False(doc.TwoParts);
            Assert.False(doc.NightLevel);
        }

        /// <summary>Leaving two-part mode promotes candyL to the single candy in place and drops candyR, so no candy is left orphaned.</summary>
        [Fact]
        public void TurningOffTwoPartsUsesCandyLAsFullCandyAndRemovesCandyR()
        {
            LevelDocument doc = LevelDocument.Parse("""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="320" height="480" />
                    <gameDesign ropePhysicsSpeed="1.0" special="0" twoParts="true" nightLevel="false" />
                </layer>
                <layer name="Objects"><candyL x="101" y="170" /><candyR x="232" y="171" /><target x="3" y="3" /></layer>
            </map>
            """);

            doc.UpdateSettings(new LevelSettings(320, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            Assert.Collection(doc.AllObjects,
                candy =>
                {
                    Assert.Equal("candy", candy.Type);
                    Assert.Equal(101, candy.X);
                    Assert.Equal(170, candy.Y);
                },
                target => Assert.Equal("target", target.Type));
        }

        /// <summary>The map element's levelName reaches the settings snapshot as the level's display name.</summary>
        [Fact]
        public void ReadsLevelName()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" levelName="Rocket Science" />
                    </layer>
                </map>
                """);

            Assert.Equal("Rocket Science", doc.LevelName);
            Assert.Equal("Rocket Science", doc.Settings.LevelName);
        }

        /// <summary>A map with no levelName reports an empty name rather than null.</summary>
        [Fact]
        public void LevelNameIsEmptyWhenAbsent()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart);

            Assert.Equal(string.Empty, doc.LevelName);
        }

        /// <summary>A named new level writes the attribute; an unnamed one leaves the map free of it.</summary>
        [Fact]
        public void CreateNewWritesLevelNameOnlyWhenSet()
        {
            LevelDocument named = LevelDocument.CreateNew(
                new LevelSettings(320, 480, 1.0f, 0, false, false, LevelName: "  Spiders  "));
            Assert.Equal("Spiders", named.LevelName);
            Assert.Contains("levelName=\"Spiders\"", named.Save());

            LevelDocument unnamed = LevelDocument.CreateNew(new LevelSettings(320, 480, 1.0f, 0, false, false));
            Assert.DoesNotContain("levelName", unnamed.Save());
        }

        /// <summary>Clearing the name removes the attribute, so a level can lose a name it once had.</summary>
        [Fact]
        public void UpdateSettingsSetsAndClearsLevelName()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart);

            doc.UpdateSettings(new LevelSettings(320, 960, 1.0f, 3, true, true, LevelName: "Bath Time"));
            Assert.Equal("Bath Time", doc.LevelName);
            Assert.Contains("levelName=\"Bath Time\"", doc.Save());

            doc.UpdateSettings(new LevelSettings(320, 960, 1.0f, 3, true, true, LevelName: "   "));
            Assert.Equal(string.Empty, doc.LevelName);
            Assert.DoesNotContain("levelName", doc.Save());
        }

        /// <summary>Gravity attributes are read as written, both axes independently.</summary>
        [Fact]
        public void ReadsGlobalGravity()
        {
            LevelDocument doc = LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="320" height="480" />
                        <gameDesign globalGravityX="-120.5" globalGravityY="0" />
                    </layer>
                </map>
                """);

            Assert.Equal(-120.5f, doc.GravityX);
            Assert.Equal(0f, doc.GravityY);
            Assert.Equal(0f, doc.Settings.GravityY);
        }

        /// <summary>Absent gravity attributes fall back to the game's defaults: none sideways, Earth downward.</summary>
        [Fact]
        public void GravityDefaultsMatchTheGame()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart);

            Assert.Equal(LevelGravity.DefaultX, doc.GravityX);
            Assert.Equal(784f, doc.GravityY);
        }

        /// <summary>Default gravity is left out of the file entirely, keeping ordinary levels free of noise.</summary>
        [Fact]
        public void CreateNewOmitsDefaultGravity()
        {
            LevelDocument doc = LevelDocument.CreateNew(new LevelSettings(320, 480, 1.0f, 0, false, false));

            Assert.DoesNotContain("globalGravity", doc.Save());
        }

        /// <summary>Non-default gravity round-trips through a save, including a deliberate weightless level.</summary>
        [Fact]
        public void UpdateSettingsWritesNonDefaultGravityIncludingZeroY()
        {
            LevelDocument doc = LevelDocument.Parse(NightTwoPart);

            doc.UpdateSettings(new LevelSettings(320, 960, 1.0f, 3, true, true, GravityX: 100f, GravityY: 0f));

            Assert.Contains("globalGravityX=\"100\"", doc.Save());
            Assert.Contains("globalGravityY=\"0\"", doc.Save());

            LevelDocument reloaded = LevelDocument.Parse(doc.Save());
            Assert.Equal(100f, reloaded.GravityX);
            Assert.Equal(0f, reloaded.GravityY);
        }

        /// <summary>Returning either axis to its default removes the attribute rather than writing it out.</summary>
        [Fact]
        public void UpdateSettingsRemovesGravityBackAtDefaults()
        {
            LevelDocument doc = LevelDocument.CreateNew(
                new LevelSettings(320, 480, 1.0f, 0, false, false, GravityX: 50f, GravityY: -784f));
            Assert.Contains("globalGravityY=\"-784\"", doc.Save());

            doc.UpdateSettings(new LevelSettings(320, 480, 1.0f, 0, false, false));

            Assert.DoesNotContain("globalGravity", doc.Save());
            Assert.Equal(LevelGravity.DefaultY, doc.GravityY);
        }

        /// <summary>Mobile physics is written out explicitly when enabled.</summary>
        [Fact]
        public void CreateNewWithMobilePhysicsWritesAttribute()
        {
            LevelDocument doc = LevelDocument.CreateNew(
                new LevelSettings(320, 480, 1.0f, 0, false, false, UseMobilePhysics: true));
            Assert.True(doc.UseMobilePhysics);
            Assert.Contains("useMobilePhysics=\"true\"", doc.Save());
        }

        /// <summary>Mobile physics is omitted rather than written false, since the game treats an absent attribute as the desktop default.</summary>
        [Fact]
        public void CreateNewWithoutMobilePhysicsOmitsAttribute()
        {
            LevelDocument doc = LevelDocument.CreateNew(
                new LevelSettings(320, 480, 1.0f, 0, false, false, UseMobilePhysics: false));
            Assert.False(doc.UseMobilePhysics);
            Assert.DoesNotContain("useMobilePhysics", doc.Save());
        }

        /// <summary>Toggling mobile physics off removes the attribute and back on restores it, so the flag survives a round trip either way.</summary>
        [Fact]
        public void UpdateSettingsTogglesMobilePhysicsBothWays()
        {
            LevelDocument doc = LevelDocument.CreateNew(
                new LevelSettings(320, 480, 1.0f, 0, false, false, UseMobilePhysics: true));
            doc.UpdateSettings(new LevelSettings(320, 480, 1.0f, 0, false, false, UseMobilePhysics: false));
            Assert.False(doc.UseMobilePhysics);
            Assert.DoesNotContain("useMobilePhysics", doc.Save());
            doc.UpdateSettings(new LevelSettings(320, 480, 1.0f, 0, false, false, UseMobilePhysics: true));
            Assert.True(doc.UseMobilePhysics);
        }

        /// <summary>Entering two-part mode demotes the existing candy to candyL and centers a fresh candyR, which lands after the objects already present.</summary>
        [Fact]
        public void TurningOnTwoPartsUsesCandyAsCandyLAndAddsCenteredCandyR()
        {
            LevelDocument doc = LevelDocument.Parse("""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                    <gameDesign ropePhysicsSpeed="1.0" special="0" twoParts="false" nightLevel="false" />
                </layer>
                <layer name="Objects"><candy x="101" y="170" /><target x="3" y="3" /></layer>
            </map>
            """);

            doc.UpdateSettings(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            Assert.Collection(doc.AllObjects,
                candyL =>
                {
                    Assert.Equal("candyL", candyL.Type);
                    Assert.Equal(101, candyL.X);
                    Assert.Equal(170, candyL.Y);
                },
                target => Assert.Equal("target", target.Type),
                candyR =>
                {
                    Assert.Equal("candyR", candyR.Type);
                    Assert.Equal(320, candyR.X);
                    Assert.Equal(240, candyR.Y);
                });
        }

        /// <summary>Two-part conversion follows candy objects into non-primary layers and keeps the pair together.</summary>
        [Fact]
        public void TwoPartConversionPreservesCandysSourceLayer()
        {
            LevelDocument doc = LevelDocument.Parse("""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                    <gameDesign twoParts="false" />
                </layer>
                <layer name="Foreground"><target x="3" y="3" /></layer>
                <layer name="Gameplay"><candy x="101" y="170" /></layer>
            </map>
            """);

            doc.UpdateSettings(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            Assert.Equal(["target"], doc.Layers[0].Objects.Select(obj => obj.Type));
            Assert.Equal(["candyL", "candyR"], doc.Layers[1].Objects.Select(obj => obj.Type));

            doc.UpdateSettings(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            Assert.Equal(["candy"], doc.Layers[1].Objects.Select(obj => obj.Type));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the bomb object: its descriptor, the bombNumber key the editor maintains, how placement,
    /// normalizing, and cloning keep grabs pointed at it, and the round trip.
    /// </summary>
    public class BombDescriptorTests
    {
        /// <summary>The bomb is a Time Travel object keyed by bombNumber, with no placement cap.</summary>
        [Fact]
        public void BombDescriptorIsATimeTravelObject()
        {
            ObjectDescriptor? bomb = DescriptorTable.CtrObjects.For("bomb");

            Assert.NotNull(bomb);
            Assert.Equal("Cut the Rope: Time Travel", bomb.Game);
            Assert.Equal(int.MaxValue, bomb.MaxCount);
            Assert.Equal("bombNumber", Assert.Single(bomb.Attributes).Name);
        }

        /// <summary>Placed bombs get ascending keys, and the key is not an editable field.</summary>
        [Fact]
        public void PlacingBombsAssignsHiddenAscendingKeys()
        {
            LevelDocument doc = NewLevel();

            Assert.Equal("0", Place(doc, "bomb").GetAttr("bombNumber"));
            Assert.Equal("1", Place(doc, "bomb").GetAttr("bombNumber"));
            Assert.False(LevelObjectPolicy.IsAttributeVisible("bomb", "bombNumber", doc));
        }

        /// <summary>With no candy yet, a grab placed after a bomb hangs from it, flag and all.</summary>
        [Fact]
        public void GrabPlacedAfterABombBindsToIt()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "bomb");

            LevelObject grab = Place(doc, "grab");

            Assert.Equal("true", grab.GetAttr("bombed"));
            Assert.Equal("0", grab.GetAttr("bombNumber"));
            Assert.Equal(RopeTargetKind.Bomb, RopeResolver.Resolve(grab, doc.AllObjects, doc.TwoParts).Kind);
        }

        /// <summary>The other order works too: a bomb placed after an unbound grab adopts it.</summary>
        [Fact]
        public void BombPlacedAfterAGrabAdoptsIt()
        {
            LevelDocument doc = NewLevel();
            LevelObject grab = Place(doc, "grab");

            _ = Place(doc, "bomb");

            Assert.Equal(RopeTargetKind.Bomb, RopeResolver.Resolve(grab, doc.AllObjects, doc.TwoParts).Kind);
        }

        /// <summary>Normalizing renumbers bombs from zero and carries their grabs along.</summary>
        [Fact]
        public void NormalizingRenumbersBombsAndRetargetsGrabs()
        {
            LevelDocument doc = NewLevel();
            LevelObject bomb = Add(doc, """<bomb x="200" y="90" bombNumber="7" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" bombed="true" bombNumber="7" />""");

            LevelObjectPolicy.NormalizeBindingKeys(doc);

            Assert.Equal("0", bomb.GetAttr("bombNumber"));
            Assert.Equal("0", grab.GetAttr("bombNumber"));
        }

        /// <summary>An imported bombed grab keyed through candyNumber is remapped against the bombs.</summary>
        [Fact]
        public void NormalizingRetargetsImportedBombedGrabsAgainstTheBombs()
        {
            LevelDocument doc = NewLevel();
            _ = Add(doc, """<candy x="178" y="178" candyNumber="4" />""");
            _ = Add(doc, """<bomb x="200" y="90" bombNumber="first" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" bombed="true" candyNumber="first" />""");

            LevelObjectPolicy.NormalizeBindingKeys(doc);

            Assert.Equal("0", grab.GetAttr("candyNumber"));
        }

        /// <summary>Cloning a bomb with its grab points the copy at the copy.</summary>
        [Fact]
        public void CloningABombWithItsGrabRetargetsTheClone()
        {
            LevelDocument doc = NewLevel();
            LevelObject bomb = Add(doc, """<bomb x="200" y="90" bombNumber="0" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" bombed="true" bombNumber="0" />""");

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([bomb, grab], ObjectLayer(doc), doc);

            Assert.Equal("1", Assert.Single(clones, o => o.Type == "bomb").GetAttr("bombNumber"));
            Assert.Equal("1", Assert.Single(clones, o => o.Type == "grab").GetAttr("bombNumber"));
            Assert.Equal("0", grab.GetAttr("bombNumber"));
        }

        /// <summary>A bombNumber that binds no bomb is reported; an imported bombed candyNumber is not a dangling candy.</summary>
        [Fact]
        public void ValidatorReportsUnmatchedBombsButNotImportedKeys()
        {
            LevelDocument doc = NewLevel();
            _ = Add(doc, """<candy x="178" y="178" candyNumber="0" />""");
            _ = Add(doc, """<target x="300" y="400" />""");
            _ = Add(doc, """<bomb x="200" y="90" bombNumber="3" />""");
            _ = Add(doc, """<grab x="181" y="87" length="55" bombed="true" candyNumber="3" />""");
            _ = Add(doc, """<grab x="240" y="87" length="55" bombNumber="3" />""");

            IReadOnlyList<LevelWarning> warnings = LevelValidator.Validate(doc);

            Assert.DoesNotContain(warnings, w => w.Key == "Validation.GrabUnmatchedCandyNumber");
            Assert.Contains(warnings, w => w.Key == "Validation.GrabUnmatchedBombNumber");
        }

        /// <summary>Bomb XML the editor did not author comes back unchanged.</summary>
        [Fact]
        public void BombAttributesSurviveARoundTrip()
        {
            string xml = """
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects"><candy x="178" y="178" candyNumber="0" /><target x="300" y="400" /><bomb x="200" y="90" bombNumber="0" /><grab x="181" y="87" length="55" bombed="true" bombNumber="0" bombsHighPriority="true" /></layer>
                </map>
                """;

            LevelDocument doc = LevelDocument.Parse(xml);

            Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(doc.Save())));
        }

        private static LevelDocument NewLevel()
        {
            return LevelDocument.Parse("""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects"></layer>
                </map>
                """);
        }

        private static LevelObject Add(LevelDocument doc, string xml)
        {
            LevelObject obj = new(XElement.Parse(xml));
            doc.Add(obj, ObjectLayer(doc));
            return obj;
        }

        private static LevelObject Place(LevelDocument doc, string element)
        {
            LevelObject obj = new(new XElement(element));
            LevelObjectPolicy.ApplyDefaults(obj, doc);
            doc.Add(obj, ObjectLayer(doc));
            return obj;
        }

        private static LevelLayer ObjectLayer(LevelDocument doc)
        {
            return doc.Layers.Single(l => l.Name == "Objects");
        }
    }
}

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
    /// Tests the axe object: its descriptor, the axeNumber key the editor assigns and maintains on the
    /// author's behalf, and the warnings raised when a binding or a chain has nothing to answer it.
    /// </summary>
    public class AxeDescriptorTests
    {
        private static ObjectDescriptor For(string element)
        {
            ObjectDescriptor? descriptor = DescriptorTable.CtrObjects.For(element);
            Assert.NotNull(descriptor);
            return descriptor;
        }

        /// <summary>The axe is a Time Travel object keyed by axeNumber, with no placement cap.</summary>
        [Fact]
        public void AxeDescriptorIsATimeTravelObject()
        {
            ObjectDescriptor axe = For("axe");

            Assert.Equal("Cut the Rope: Time Travel", axe.Game);
            Assert.Equal(int.MaxValue, axe.MaxCount);
            AttributeSpec key = Assert.Single(axe.Attributes);
            Assert.Equal("axeNumber", key.Name);
            Assert.Equal(AttrType.Text, key.Type);
        }

        /// <summary>An axe's key is auto-assigned on placement, so nobody has to type one.</summary>
        [Fact]
        public void PlacingAxesAssignsAscendingKeys()
        {
            LevelDocument doc = NewLevel();

            LevelObject first = Place(doc, "axe");
            LevelObject second = Place(doc, "axe");

            Assert.Equal("0", first.GetAttr("axeNumber"));
            Assert.Equal("1", second.GetAttr("axeNumber"));
        }

        /// <summary>The axe key is authored internally, so it is not offered as an editable field.</summary>
        [Fact]
        public void AxeNumberIsNotAnEditableField()
        {
            Assert.False(LevelObjectPolicy.IsAttributeVisible("axe", "axeNumber", NewLevel()));
        }

        /// <summary>Normalizing renumbers axes from zero and carries their grabs along.</summary>
        [Fact]
        public void NormalizingRenumbersAxesAndRetargetsGrabs()
        {
            LevelDocument doc = NewLevel();
            LevelObject axe = Add(doc, """<axe x="200" y="90" axeNumber="7" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" axeNumber="7" />""");

            LevelObjectPolicy.NormalizeBindingKeys(doc);

            Assert.Equal("0", axe.GetAttr("axeNumber"));
            Assert.Equal("0", grab.GetAttr("axeNumber"));
        }

        /// <summary>
        /// An imported axed="true" grab keeps its key in candyNumber, so normalizing has to remap that
        /// attribute against the axes rather than against the candies.
        /// </summary>
        [Fact]
        public void NormalizingRetargetsLegacyAxedGrabsAgainstTheAxes()
        {
            LevelDocument doc = NewLevel();
            _ = Add(doc, """<candy x="178" y="178" candyNumber="4" />""");
            LevelObject axe = Add(doc, """<axe x="200" y="90" axeNumber="7" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" axed="true" candyNumber="7" />""");

            LevelObjectPolicy.NormalizeBindingKeys(doc);

            Assert.Equal("0", axe.GetAttr("axeNumber"));
            Assert.Equal("0", grab.GetAttr("candyNumber"));
        }

        /// <summary>Cloning an axe with its grab points the copy at the copy, not at the original.</summary>
        [Fact]
        public void CloningAnAxeWithItsGrabRetargetsTheClone()
        {
            LevelDocument doc = NewLevel();
            LevelObject axe = Add(doc, """<axe x="200" y="90" axeNumber="0" />""");
            LevelObject grab = Add(doc, """<grab x="181" y="87" length="55" axeNumber="0" />""");

            IReadOnlyList<LevelObject> clones = ObjectCloneService.Clone([axe, grab], doc.Layers[0], doc);

            LevelObject clonedAxe = Assert.Single(clones, o => o.Type == "axe");
            LevelObject clonedGrab = Assert.Single(clones, o => o.Type == "grab");
            Assert.Equal("1", clonedAxe.GetAttr("axeNumber"));
            Assert.Equal("1", clonedGrab.GetAttr("axeNumber"));
            Assert.Equal("0", axe.GetAttr("axeNumber"));
            Assert.Equal("0", grab.GetAttr("axeNumber"));
        }

        /// <summary>A chain with no axe to cut it makes the level unwinnable, and is called out.</summary>
        [Fact]
        public void ChainWithoutAnAxeWarns()
        {
            LevelDocument doc = PlayableLevel();
            _ = Add(doc, """<grab x="181" y="87" length="55" breakable="false" />""");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.ChainWithoutAxe");
        }

        /// <summary>An axe in the level settles the chain, so no warning is raised.</summary>
        [Fact]
        public void ChainWithAnAxeDoesNotWarn()
        {
            LevelDocument doc = PlayableLevel();
            _ = Add(doc, """<axe x="200" y="90" axeNumber="0" />""");
            _ = Add(doc, """<grab x="181" y="87" length="55" breakable="false" />""");

            Assert.DoesNotContain(LevelValidator.Validate(doc), w => w.Key == "Validation.ChainWithoutAxe");
        }

        /// <summary>An axeNumber no axe answers to binds the candy instead, which is worth saying.</summary>
        [Fact]
        public void UnmatchedAxeNumberWarns()
        {
            LevelDocument doc = PlayableLevel();
            _ = Add(doc, """<axe x="200" y="90" axeNumber="0" />""");
            _ = Add(doc, """<grab x="181" y="87" length="55" axeNumber="9" />""");

            Assert.Contains(LevelValidator.Validate(doc), w => w.Key == "Validation.GrabUnmatchedAxeNumber");
        }

        /// <summary>
        /// A legacy axed grab's candyNumber names an axe, not a candy, so it must not be reported as a
        /// dangling candy reference.
        /// </summary>
        [Fact]
        public void LegacyAxedGrabIsNotReportedAsAnUnmatchedCandy()
        {
            LevelDocument doc = PlayableLevel();
            _ = Add(doc, """<axe x="200" y="90" axeNumber="3" />""");
            _ = Add(doc, """<grab x="181" y="87" length="55" axed="true" candyNumber="3" />""");

            Assert.DoesNotContain(
                LevelValidator.Validate(doc), w => w.Key == "Validation.GrabUnmatchedCandyNumber");
        }

        /// <summary>
        /// The editor keeps XML it does not author. A hand-written level using an axe, an explicit
        /// axeNumber, and an imported axed grab comes back unchanged.
        /// </summary>
        [Fact]
        public void AxeAttributesSurviveARoundTrip()
        {
            string xml = """
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects"><candy x="178" y="178" candyNumber="0" /><target x="300" y="400" /><axe x="200" y="90" axeNumber="0" /><grab x="181" y="87" length="55" axeNumber="0" /><grab x="240" y="87" length="55" axed="true" candyNumber="0" /></layer>
                </map>
                """;

            LevelDocument doc = LevelDocument.Parse(xml);

            Assert.True(XNode.DeepEquals(XDocument.Parse(xml), XDocument.Parse(doc.Save())));
        }

        private static LevelDocument NewLevel(string objects = "")
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign />
                    </layer>
                    <layer name="Objects">{objects}</layer>
                </map>
                """);
        }

        private static LevelDocument PlayableLevel()
        {
            LevelDocument doc = NewLevel();
            _ = Add(doc, """<candy x="178" y="178" candyNumber="0" />""");
            _ = Add(doc, """<target x="300" y="400" />""");
            return doc;
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

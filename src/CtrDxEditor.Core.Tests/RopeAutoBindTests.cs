using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the rope's placement defaults. Until a level has a candy, a grab has nothing to hang from
    /// unless it is told about a blade or a bulb, so the editor authors that binding as objects are
    /// placed - in either order - and stops as soon as a candy makes the game's own default meaningful.
    /// </summary>
    public class RopeAutoBindTests
    {
        /// <summary>A grab placed after a blade takes the blade as its rope target.</summary>
        [Fact]
        public void GrabPlacedAfterABladeBindsToIt()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "axe");

            LevelObject grab = Place(doc, "grab");

            Assert.Equal("0", grab.GetAttr("axeNumber"));
            Assert.Equal(RopeTargetKind.Axe, Resolve(doc, grab).Kind);
        }

        /// <summary>A grab placed after a bulb binds to it, which needs bindBulb as well as the key.</summary>
        [Fact]
        public void GrabPlacedAfterABulbBindsToIt()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "lightBulb");

            LevelObject grab = Place(doc, "grab");

            Assert.Equal("true", grab.GetAttr("bindBulb"));
            Assert.Equal("0", grab.GetAttr("bulbNumber"));
            Assert.Equal(RopeTargetKind.Bulb, Resolve(doc, grab).Kind);
        }

        /// <summary>The other order works too: a blade placed after a grab adopts it.</summary>
        [Fact]
        public void BladePlacedAfterAGrabAdoptsIt()
        {
            LevelDocument doc = NewLevel();
            LevelObject grab = Place(doc, "grab");
            Assert.Null(Resolve(doc, grab).Target);

            _ = Place(doc, "axe");

            Assert.Equal("0", grab.GetAttr("axeNumber"));
            Assert.Equal(RopeTargetKind.Axe, Resolve(doc, grab).Kind);
        }

        /// <summary>A bulb placed after a grab adopts it the same way.</summary>
        [Fact]
        public void BulbPlacedAfterAGrabAdoptsIt()
        {
            LevelDocument doc = NewLevel();
            LevelObject grab = Place(doc, "grab");

            _ = Place(doc, "lightBulb");

            Assert.Equal("true", grab.GetAttr("bindBulb"));
            Assert.Equal(RopeTargetKind.Bulb, Resolve(doc, grab).Kind);
        }

        /// <summary>Whichever was placed first is the one a later grab hangs from.</summary>
        [Fact]
        public void TheFirstTargetPlacedWins()
        {
            LevelDocument bladeFirst = NewLevel();
            _ = Place(bladeFirst, "axe");
            _ = Place(bladeFirst, "lightBulb");
            Assert.Equal("0", Place(bladeFirst, "grab").GetAttr("axeNumber"));

            LevelDocument bulbFirst = NewLevel();
            _ = Place(bulbFirst, "lightBulb");
            _ = Place(bulbFirst, "axe");
            LevelObject grab = Place(bulbFirst, "grab");
            Assert.Equal("true", grab.GetAttr("bindBulb"));
            Assert.Null(grab.GetAttr("axeNumber"));
        }

        /// <summary>
        /// Once a candy exists the game's own default applies, so nothing is authored: a grab binds to
        /// the primary candy without being told to.
        /// </summary>
        [Fact]
        public void ACandyInTheLevelStopsTheAutoBinding()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "candy");
            _ = Place(doc, "axe");

            LevelObject grab = Place(doc, "grab");

            Assert.Null(grab.GetAttr("axeNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
            Assert.Equal(RopeTargetKind.Candy, Resolve(doc, grab).Kind);
        }

        /// <summary>A blade placed into a level that has a candy leaves the existing grabs alone.</summary>
        [Fact]
        public void ABladeDoesNotAdoptGrabsOnceACandyExists()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "candy");
            LevelObject grab = Place(doc, "grab");

            _ = Place(doc, "axe");

            Assert.Null(grab.GetAttr("axeNumber"));
        }

        /// <summary>
        /// A gun or auto-catch hook takes hold during play and LoadGrabs skips its binding block, so
        /// writing a target on one would only add XML the game ignores.
        /// </summary>
        [Theory]
        [InlineData("gun", "true")]
        [InlineData("radius", "100")]
        public void GrabsWithoutAnAuthoredRopeAreLeftAlone(string attribute, string value)
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "axe");

            LevelObject grab = new(new XElement("grab", new XAttribute(attribute, value)));
            LevelObjectPolicy.ApplyDefaults(grab, doc);

            Assert.Null(grab.GetAttr("axeNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
        }

        /// <summary>A grab already hanging from something keeps that target when another is placed.</summary>
        [Fact]
        public void AnAlreadyBoundGrabIsNotRetargeted()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "axe");
            LevelObject grab = Place(doc, "grab");

            _ = Place(doc, "lightBulb");

            Assert.Equal("0", grab.GetAttr("axeNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
        }

        /// <summary>
        /// A clone arrives already carrying the binding it was copied from, so it keeps that target
        /// rather than being pulled onto whichever blade happens to be first.
        /// </summary>
        [Fact]
        public void ACloneKeepsTheTargetItWasCopiedFrom()
        {
            LevelDocument doc = NewLevel();
            _ = Place(doc, "axe");
            LevelObject second = Place(doc, "axe");
            LevelObject grab = Place(doc, "grab");
            GrabBinding.Apply(grab, $"axe:{AxeBinding.KeyOf(second)}");

            IReadOnlyList<LevelObject> clones =
                ObjectCloneService.Clone([grab], doc.Layers.Single(l => l.Name == "Objects"), doc);

            Assert.Equal("1", Assert.Single(clones).GetAttr("axeNumber"));
        }

        /// <summary>Two-part levels behave the same: their halves are the candy that stops the binding.</summary>
        [Fact]
        public void TwoPartLevelsCountTheirHalvesAsCandy()
        {
            LevelDocument withHalf = NewLevel(twoParts: true, objects: """<candyL x="1" y="1" />""");
            _ = Place(withHalf, "axe");
            Assert.Null(Place(withHalf, "grab").GetAttr("axeNumber"));

            LevelDocument withoutHalf = NewLevel(twoParts: true);
            _ = Place(withoutHalf, "axe");
            Assert.Equal("0", Place(withoutHalf, "grab").GetAttr("axeNumber"));
        }

        /// <summary>
        /// The "Attach to" control appears whenever the level offers a target a grab would not take on
        /// its own. A lone candy is that default, so it needs no control; a lone blade or bulb does.
        /// </summary>
        [Fact]
        public void AttachToIsOfferedForALoneBladeOrBulbButNotALoneCandy()
        {
            Assert.False(GrabBinding.OffersAChoice(Options("""<candy x="1" y="1" />""")));
            Assert.False(GrabBinding.OffersAChoice(Options()));
            Assert.True(GrabBinding.OffersAChoice(Options("""<axe x="1" y="1" axeNumber="0" />""")));
            Assert.True(GrabBinding.OffersAChoice(Options("""<lightBulb x="1" y="1" bulbNumber="0" />""")));
            Assert.True(GrabBinding.OffersAChoice(
                Options("""<candy x="1" y="1" />""", """<axe x="2" y="2" axeNumber="0" />""")));
        }

        private static IReadOnlyList<GrabBindOption> Options(params string[] xml)
        {
            return GrabBinding.Options([.. xml.Select(x => new LevelObject(XElement.Parse(x)))], twoParts: false);
        }

        private static RopeTarget Resolve(LevelDocument doc, LevelObject grab)
        {
            return RopeResolver.Resolve(grab, doc.AllObjects, doc.TwoParts);
        }

        private static LevelDocument NewLevel(bool twoParts = false, string objects = "")
        {
            string flags = twoParts ? "twoParts=\"true\"" : "";
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign {flags} />
                    </layer>
                    <layer name="Objects">{objects}</layer>
                </map>
                """);
        }

        private static LevelObject Place(LevelDocument doc, string element)
        {
            LevelObject obj = new(new XElement(element));
            LevelObjectPolicy.ApplyDefaults(obj, doc);
            doc.Add(obj, doc.Layers.Single(l => l.Name == "Objects"));
            return obj;
        }
    }
}

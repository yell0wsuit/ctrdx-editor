using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests binding a grab's rope to an axe, against the game's <c>LoadGrabs</c> / <c>AxeGrabBinding</c>:
    /// an explicit <c>axeNumber</c> wins, an imported <c>axed="true"</c> reuses <c>candyNumber</c>, and an
    /// axe target outranks a candy one.
    /// </summary>
    public class AxeBindingTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        /// <summary>An explicit axeNumber names the axe to bind to.</summary>
        [Fact]
        public void ExplicitAxeNumberIsTheRequestedKey()
        {
            Assert.Equal("2", AxeBinding.RequestedKey(Obj("""<grab x="1" y="1" axeNumber="2" />""")));
        }

        /// <summary>An imported axed="true" grab takes its axe key from candyNumber.</summary>
        [Fact]
        public void LegacyAxedFlagFallsBackToCandyNumber()
        {
            Assert.Equal("1", AxeBinding.RequestedKey(Obj("""<grab x="1" y="1" axed="true" candyNumber="1" />""")));
        }

        /// <summary>An explicit axeNumber wins over the imported flag's candyNumber.</summary>
        [Fact]
        public void ExplicitKeyWinsOverTheLegacyFlag()
        {
            Assert.Equal("7", AxeBinding.RequestedKey(Obj("""<grab x="1" y="1" axed="true" candyNumber="1" axeNumber="7" />""")));
        }

        /// <summary>A plain grab asks for no axe at all, so candyNumber stays a candy reference.</summary>
        [Fact]
        public void PlainGrabRequestsNoAxe()
        {
            Assert.Null(AxeBinding.RequestedKey(Obj("""<grab x="1" y="1" candyNumber="1" />""")));
        }

        /// <summary>LoadAxe reads a missing axeNumber as the empty string, not as "no key".</summary>
        [Fact]
        public void AxeWithoutAKeyIsKeyedByTheEmptyString()
        {
            Assert.Equal(string.Empty, AxeBinding.KeyOf(Obj("""<axe x="1" y="1" />""")));
            Assert.True(AxeBinding.KeyEquals(AxeBinding.KeyOf(Obj("""<axe x="1" y="1" />""")), ""));
        }

        /// <summary>Keys match the way CandyMatch does: trimmed and case-insensitive, both present.</summary>
        [Theory]
        [InlineData("A", " a ", true)]
        [InlineData("0", "0", true)]
        [InlineData("0", "1", false)]
        [InlineData(null, "0", false)]
        [InlineData("0", null, false)]
        public void KeyEqualsMatchesCandyMatchSemantics(string? a, string? b, bool expected)
        {
            Assert.Equal(expected, AxeBinding.KeyEquals(a, b));
        }

        /// <summary>A grab naming an axe ropes the axe.</summary>
        [Fact]
        public void GrabWithAxeNumberResolvesToTheAxe()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="0" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" axeNumber="0" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, axe, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Axe, t.Kind);
            Assert.Same(axe, t.Target);
        }

        /// <summary>An axe target beats a candy one, matching LoadGrabs' branch order.</summary>
        [Fact]
        public void AxeTargetOutranksCandyTarget()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="1" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="1" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" candyNumber="1" axeNumber="1" />""");

            Assert.Same(axe, RopeResolver.Resolve(grab, [candy, axe, grab], twoParts: false).Target);
        }

        /// <summary>An imported axed grab resolves to the axe its candyNumber names.</summary>
        [Fact]
        public void LegacyAxedGrabResolvesToTheAxe()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="3" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" axed="true" candyNumber="3" />""");

            Assert.Same(axe, RopeResolver.Resolve(grab, [candy, axe, grab], twoParts: false).Target);
        }

        /// <summary>An axeNumber no axe answers to falls back to the candy, as the game does.</summary>
        [Fact]
        public void UnmatchedAxeNumberFallsBackToTheCandy()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="0" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" axeNumber="9" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, axe, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candy, t.Target);
        }

        /// <summary>
        /// bindBulb is read before the axe in LoadGrabs, and a bindBulb grab with no bulbs falls back to
        /// the candy rather than reaching the axe branch at all.
        /// </summary>
        [Fact]
        public void BindBulbNeverFallsThroughToAnAxe()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="0" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" bindBulb="true" bulbNumber="0" axeNumber="0" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, axe, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candy, t.Target);
        }

        /// <summary>A gun or auto-catch grab has no authored rope, so it resolves to no target at all.</summary>
        [Theory]
        [InlineData("""<grab x="181" y="87" gun="true" axeNumber="0" />""")]
        [InlineData("""<grab x="181" y="87" radius="100" axeNumber="0" />""")]
        public void GrabsWithoutAnAuthoredRopeResolveToNothing(string xml)
        {
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="0" />""");
            LevelObject grab = Obj(xml);

            Assert.Equal(RopeTargetKind.None, RopeResolver.Resolve(grab, [axe, grab], twoParts: false).Kind);
        }

        /// <summary>The "Attach to" list offers every axe in the level alongside candies and bulbs.</summary>
        [Fact]
        public void OptionsOfferEachAxe()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe0 = Obj("""<axe x="200" y="90" axeNumber="0" />""");
            LevelObject axe1 = Obj("""<axe x="220" y="90" axeNumber="1" />""");

            IReadOnlyList<GrabBindOption> options = GrabBinding.Options([candy, axe0, axe1], twoParts: false);

            // The token keeps the game's spelling; only the label the author reads says "Blade".
            Assert.Contains(options, o => o.Token == "axe:0" && o.Label == "Blade 0");
            Assert.Contains(options, o => o.Token == "axe:1" && o.Label == "Blade 1");
        }

        /// <summary>Selecting an axe writes only the explicit key and clears every other target.</summary>
        [Fact]
        public void ApplyAxeTokenWritesTheKeyAndClearsOtherTargets()
        {
            LevelObject grab = Obj("""<grab x="1" y="1" candyNumber="2" bindBulb="true" bulbNumber="1" axed="true" />""");

            GrabBinding.Apply(grab, "axe:4");

            Assert.Equal("4", grab.GetAttr("axeNumber"));
            Assert.Null(grab.GetAttr("candyNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
            Assert.Null(grab.GetAttr("bulbNumber"));
            Assert.Null(grab.GetAttr("axed"));
        }

        /// <summary>Switching away from an axe drops both spellings, so nothing re-captures the grab.</summary>
        [Theory]
        [InlineData("primary")]
        [InlineData("candy:1")]
        [InlineData("bulb:0")]
        public void ApplyingAnotherTargetClearsBothAxeSpellings(string token)
        {
            LevelObject grab = Obj("""<grab x="1" y="1" axeNumber="4" axed="true" />""");

            GrabBinding.Apply(grab, token);

            Assert.Null(grab.GetAttr("axeNumber"));
            Assert.Null(grab.GetAttr("axed"));
        }

        /// <summary>A grab bound to an axe reports that axe as its current selection.</summary>
        [Fact]
        public void CurrentTokenReportsTheAxe()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject axe = Obj("""<axe x="200" y="90" axeNumber="1" />""");
            LevelObject grab = Obj("""<grab x="1" y="1" axeNumber="1" />""");

            Assert.Equal("axe:1", GrabBinding.CurrentToken(grab, [candy, axe, grab], twoParts: false));
        }

        /// <summary>An axeNumber no axe answers to reads back as the candy the game would fall back to.</summary>
        [Fact]
        public void CurrentTokenFallsBackWhenNoAxeMatches()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" candyNumber="0" />""");
            LevelObject grab = Obj("""<grab x="1" y="1" axeNumber="9" />""");

            Assert.Equal("primary", GrabBinding.CurrentToken(grab, [candy, grab], twoParts: false));
        }
    }
}

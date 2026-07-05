using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for resolving visual rope targets from grab objects.</summary>
    public class RopeResolverTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        /// <summary>Verifies that a normal grab connects to the single candy in one-part levels.</summary>
        [Fact]
        public void SingleCandyGrabBindsToCandy()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candy, t.Target);
        }

        /// <summary>Verifies that two-part levels resolve a grab to the candy matching its part.</summary>
        [Fact]
        public void TwoPartGrabBindsByPart()
        {
            LevelObject candyL = Obj("""<candyL x="101" y="170" />""");
            LevelObject candyR = Obj("""<candyR x="232" y="171" />""");
            LevelObject grabR = Obj("""<grab x="164" y="146" length="150" part="R" />""");

            RopeTarget t = RopeResolver.Resolve(grabR, [candyL, candyR, grabR], twoParts: true);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candyR, t.Target);
        }

        /// <summary>Verifies that gun grabs do not draw a rope target.</summary>
        [Fact]
        public void GunGrabHasNoRope()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" />""");
            LevelObject gun = Obj("""<grab x="10" y="10" gun="true" />""");

            Assert.Equal(RopeTargetKind.None,
                RopeResolver.Resolve(gun, [candy, gun], twoParts: false).Kind);
        }

        /// <summary>Verifies that bulb-bound grabs connect to the matching numbered light bulb.</summary>
        [Fact]
        public void BindBulbGrabLinksToMatchingBulb()
        {
            LevelObject bulb = Obj("""<lightBulb x="50" y="50" number="2" />""");
            LevelObject grab = Obj("""<grab x="10" y="10" bindBulb="true" bulbNumber="2" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [bulb, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Bulb, t.Kind);
            Assert.Same(bulb, t.Target);
        }

        /// <summary>Verifies that single-candy grabs can target a candy by candyNumber.</summary>
        [Fact]
        public void GrabBindsToCandyByNumber()
        {
            LevelObject c0 = Obj("""<candy x="10" y="10" candyNumber="0" />""");
            LevelObject c1 = Obj("""<candy x="20" y="20" candyNumber="1" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" length="50" candyNumber="1" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [c0, c1, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(c1, t.Target);
        }

        /// <summary>Unmatched candyNumber references fall back to the primary candy.</summary>
        [Fact]
        public void UnmatchedCandyNumberFallsBackToPrimary()
        {
            LevelObject c0 = Obj("""<candy x="10" y="10" candyNumber="0" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" length="50" candyNumber="9" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [c0, grab], twoParts: false);

            Assert.Same(c0, t.Target);
        }

        /// <summary>Unmatched bulb numbers fall back to the last light bulb in the level.</summary>
        [Fact]
        public void BindBulbFallsBackToLastBulbWhenNumberUnmatched()
        {
            LevelObject b0 = Obj("""<lightBulb x="1" y="1" bulbNumber="0" />""");
            LevelObject b1 = Obj("""<lightBulb x="2" y="2" bulbNumber="1" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" bindBulb="true" bulbNumber="9" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [b0, b1, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Bulb, t.Kind);
            Assert.Same(b1, t.Target);
        }

        /// <summary>Bulb-bound grabs fall back to candy resolution when no bulbs exist.</summary>
        [Fact]
        public void BindBulbWithNoBulbsFallsBackToCandy()
        {
            LevelObject candy = Obj("""<candy x="10" y="10" candyNumber="0" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" bindBulb="true" bulbNumber="0" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candy, t.Target);
        }
    }
}

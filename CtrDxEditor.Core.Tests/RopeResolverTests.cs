using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class RopeResolverTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        [Fact]
        public void SingleCandyGrabBindsToCandy()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" />""");
            LevelObject grab = Obj("""<grab x="181" y="87" length="55" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [candy, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, t.Kind);
            Assert.Same(candy, t.Target);
        }

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

        [Fact]
        public void GunGrabHasNoRope()
        {
            LevelObject candy = Obj("""<candy x="178" y="178" />""");
            LevelObject gun = Obj("""<grab x="10" y="10" gun="true" />""");

            Assert.Equal(RopeTargetKind.None,
                RopeResolver.Resolve(gun, [candy, gun], twoParts: false).Kind);
        }

        [Fact]
        public void BindBulbGrabLinksToMatchingBulb()
        {
            LevelObject bulb = Obj("""<lightBulb x="50" y="50" number="2" />""");
            LevelObject grab = Obj("""<grab x="10" y="10" bindBulb="true" bulbNumber="2" />""");

            RopeTarget t = RopeResolver.Resolve(grab, [bulb, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Bulb, t.Kind);
            Assert.Same(bulb, t.Target);
        }
    }
}

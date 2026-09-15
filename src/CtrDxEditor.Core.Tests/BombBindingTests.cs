using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests binding a grab's rope to a bomb, against the game's <c>LoadGrabs</c> / <c>BombGrabBinding</c>:
    /// only a <c>bombed="true"</c> grab looks a bomb up, an explicit <c>bombNumber</c> wins over the
    /// imported <c>candyNumber</c> key, and a bomb target outranks both an axe and a candy.
    /// </summary>
    public class BombBindingTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        /// <summary>A bombed grab's explicit bombNumber names the bomb.</summary>
        [Fact]
        public void BombedGrabRequestsItsBombNumber()
        {
            Assert.Equal("2", BombBinding.RequestedKey(Obj("""<grab x="1" y="1" bombed="true" bombNumber="2" />""")));
        }

        /// <summary>Without bombed="true" the game never looks for a bomb, key or not.</summary>
        [Fact]
        public void BombNumberWithoutTheFlagRequestsNothing()
        {
            Assert.Null(BombBinding.RequestedKey(Obj("""<grab x="1" y="1" bombNumber="2" />""")));
        }

        /// <summary>An imported bombed grab without a bombNumber takes its key from candyNumber.</summary>
        [Fact]
        public void BombedGrabFallsBackToCandyNumber()
        {
            Assert.Equal("first", BombBinding.RequestedKey(Obj("""<grab x="1" y="1" bombed="1" candyNumber="first" />""")));
        }

        /// <summary>A bombed grab hangs from the matching bomb even when a candy shares the key.</summary>
        [Fact]
        public void ResolverPrefersTheBombOverTheCandy()
        {
            LevelObject candy = Obj("""<candy x="1" y="1" candyNumber="first" />""");
            LevelObject bomb = Obj("""<bomb x="5" y="5" bombNumber="first" />""");
            LevelObject grab = Obj("""<grab x="9" y="9" bombed="true" candyNumber="first" />""");

            RopeTarget target = RopeResolver.Resolve(grab, [candy, bomb, grab], twoParts: false);

            Assert.Equal(RopeTargetKind.Bomb, target.Kind);
            Assert.Same(bomb, target.Target);
        }

        /// <summary>LoadGrabs tries the bomb before the axe, so a grab naming both hangs from the bomb.</summary>
        [Fact]
        public void ResolverPrefersTheBombOverTheAxe()
        {
            LevelObject axe = Obj("""<axe x="1" y="1" axeNumber="0" />""");
            LevelObject bomb = Obj("""<bomb x="5" y="5" bombNumber="0" />""");
            LevelObject grab = Obj("""<grab x="9" y="9" axeNumber="0" bombed="true" bombNumber="0" />""");

            Assert.Equal(RopeTargetKind.Bomb, RopeResolver.Resolve(grab, [axe, bomb, grab], twoParts: false).Kind);
        }

        /// <summary>An unbombed grab keeps binding the candy, as the game's UnbombedGrabStillBindsToTheCandy asserts.</summary>
        [Fact]
        public void UnbombedGrabStillBindsToTheCandy()
        {
            LevelObject candy = Obj("""<candy x="1" y="1" candyNumber="first" />""");
            LevelObject bomb = Obj("""<bomb x="5" y="5" bombNumber="first" />""");
            LevelObject grab = Obj("""<grab x="9" y="9" candyNumber="first" />""");

            Assert.Equal(RopeTargetKind.Candy, RopeResolver.Resolve(grab, [candy, bomb, grab], twoParts: false).Kind);
        }

        /// <summary>Picking a bomb under "Attach to" writes both attributes LoadGrabs needs.</summary>
        [Fact]
        public void ApplyingABombTokenWritesTheFlagAndKey()
        {
            LevelObject grab = Obj("""<grab x="9" y="9" candyNumber="0" axeNumber="1" />""");

            GrabBinding.Apply(grab, "bomb:3");

            Assert.Equal("true", grab.GetAttr("bombed"));
            Assert.Equal("3", grab.GetAttr("bombNumber"));
            Assert.Null(grab.GetAttr("candyNumber"));
            Assert.Null(grab.GetAttr("axeNumber"));
        }

        /// <summary>Switching away from a bomb drops the flag too, so it cannot re-capture a later candyNumber.</summary>
        [Fact]
        public void SwitchingAwayFromABombClearsTheFlag()
        {
            LevelObject grab = Obj("""<grab x="9" y="9" bombed="true" bombNumber="3" />""");

            GrabBinding.Apply(grab, "candy:1");

            Assert.Null(grab.GetAttr("bombed"));
            Assert.Null(grab.GetAttr("bombNumber"));
            Assert.Equal("1", grab.GetAttr("candyNumber"));
        }

        /// <summary>A bomb is offered under "Attach to", and a bound grab reads back as that bomb.</summary>
        [Fact]
        public void BombIsOfferedAndReadBackAsTheCurrentTarget()
        {
            LevelObject candy = Obj("""<candy x="1" y="1" candyNumber="0" />""");
            LevelObject bomb = Obj("""<bomb x="5" y="5" bombNumber="0" />""");
            LevelObject grab = Obj("""<grab x="9" y="9" bombed="true" bombNumber="0" />""");
            IReadOnlyList<LevelObject> objects = [candy, bomb, grab];

            Assert.Contains(GrabBinding.Options(objects, twoParts: false), o => o.Token == "bomb:0");
            Assert.Equal("bomb:0", GrabBinding.CurrentToken(grab, objects, twoParts: false));
        }
    }
}

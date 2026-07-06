using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the grab "Attach to" option/current/apply model.</summary>
    public class GrabBindingTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        /// <summary>Multi-candy options list the primary candy first, then numbered extra candies.</summary>
        [Fact]
        public void MultiCandyOptionsListPrimaryThenNumbered()
        {
            LevelObject c0 = Obj("""<candy x="1" y="1" candyNumber="0" />""");
            LevelObject c1 = Obj("""<candy x="2" y="2" candyNumber="1" />""");

            IReadOnlyList<GrabBindOption> options = GrabBinding.Options([c0, c1], twoParts: false);

            Assert.Equal(["primary", "candy:1"], options.Select(o => o.Token));
        }

        /// <summary>Two-part levels expose left and right candy attach options.</summary>
        [Fact]
        public void TwoPartsOptionsAreLeftRight()
        {
            LevelObject l = Obj("""<candyL x="1" y="1" />""");
            LevelObject r = Obj("""<candyR x="2" y="2" />""");

            IReadOnlyList<GrabBindOption> options = GrabBinding.Options([l, r], twoParts: true);

            Assert.Equal(["part:L", "part:R"], options.Select(o => o.Token));
        }

        /// <summary>Bulb options are appended after candy options.</summary>
        [Fact]
        public void BulbsAppendedInBothModes()
        {
            LevelObject c0 = Obj("""<candy x="1" y="1" candyNumber="0" />""");
            LevelObject b = Obj("""<lightBulb x="3" y="3" bulbNumber="0" />""");

            IReadOnlyList<GrabBindOption> options = GrabBinding.Options([c0, b], twoParts: false);

            Assert.Equal(["primary", "bulb:0"], options.Select(o => o.Token));
        }

        /// <summary>Applying a candy option writes candyNumber and clears bulb binding attributes.</summary>
        [Fact]
        public void ApplyCandyWritesNumberAndClearsBulb()
        {
            LevelObject grab = Obj("""<grab x="5" y="5" bindBulb="true" bulbNumber="0" />""");

            GrabBinding.Apply(grab, "candy:2");

            Assert.Equal("2", grab.GetAttr("candyNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
            Assert.Null(grab.GetAttr("bulbNumber"));
        }

        /// <summary>Applying primary clears all explicit binding attributes.</summary>
        [Fact]
        public void ApplyPrimaryClearsBindingAttrs()
        {
            LevelObject grab = Obj("""<grab x="5" y="5" candyNumber="3" />""");

            GrabBinding.Apply(grab, "primary");

            Assert.Null(grab.GetAttr("candyNumber"));
            Assert.Null(grab.GetAttr("bindBulb"));
        }

        /// <summary>Applying a bulb option sets bindBulb and clears candyNumber.</summary>
        [Fact]
        public void ApplyBulbSetsBindBulbAndClearsCandyNumber()
        {
            LevelObject grab = Obj("""<grab x="5" y="5" candyNumber="1" />""");

            GrabBinding.Apply(grab, "bulb:0");

            Assert.Equal("true", grab.GetAttr("bindBulb"));
            Assert.Equal("0", grab.GetAttr("bulbNumber"));
            Assert.Null(grab.GetAttr("candyNumber"));
        }

        /// <summary>The current token reflects a grab bound to a non-primary candyNumber.</summary>
        [Fact]
        public void CurrentTokenReflectsCandyNumber()
        {
            LevelObject c0 = Obj("""<candy x="1" y="1" candyNumber="0" />""");
            LevelObject c1 = Obj("""<candy x="2" y="2" candyNumber="1" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" candyNumber="1" />""");

            Assert.Equal("candy:1", GrabBinding.CurrentToken(grab, [c0, c1, grab], twoParts: false));
        }

        /// <summary>A grab without candyNumber selects the primary candy token.</summary>
        [Fact]
        public void CurrentTokenIsPrimaryWhenNoCandyNumber()
        {
            LevelObject c0 = Obj("""<candy x="1" y="1" candyNumber="0" />""");
            LevelObject grab = Obj("""<grab x="5" y="5" />""");

            Assert.Equal("primary", GrabBinding.CurrentToken(grab, [c0, grab], twoParts: false));
        }
    }
}

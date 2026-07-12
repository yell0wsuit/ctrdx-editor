using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests lantern state helpers.</summary>
    public class LanternObjectTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        [Fact]
        public void IsCapturedReadsAttribute()
        {
            Assert.True(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" candyCaptured="true" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" candyCaptured="false" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<candy x="0" y="0" />""")));
        }

        [Fact]
        public void SpriteKeyReflectsCaptureState()
        {
            Assert.Equal("lantern_active", LanternObject.SpriteKey(Obj("""<lantern x="0" y="0" candyCaptured="true" />""")));
            Assert.Equal("lantern", LanternObject.SpriteKey(Obj("""<lantern x="0" y="0" candyCaptured="false" />""")));
        }

        [Fact]
        public void AnyCapturedScansTheLevel()
        {
            List<LevelObject> objs =
            [
                Obj("""<candy x="0" y="0" />"""),
                Obj("""<lantern x="1" y="1" candyCaptured="false" />"""),
                Obj("""<lantern x="2" y="2" candyCaptured="true" />"""),
            ];
            Assert.True(LanternObject.AnyCaptured(objs));

            List<LevelObject> none =
            [
                Obj("""<lantern x="1" y="1" candyCaptured="false" />"""),
            ];
            Assert.False(LanternObject.AnyCaptured(none));
        }

        [Fact]
        public void IsPrimaryCandyIsFirstCandyInDocumentOrder()
        {
            LevelObject first = Obj("""<candy x="0" y="0" />""");
            LevelObject second = Obj("""<candy x="9" y="9" candyNumber="1" />""");
            List<LevelObject> objs = [Obj("""<lantern x="1" y="1" candyCaptured="true" />"""), first, second];

            Assert.True(LanternObject.IsPrimaryCandy(first, objs));
            Assert.False(LanternObject.IsPrimaryCandy(second, objs));
        }
    }
}

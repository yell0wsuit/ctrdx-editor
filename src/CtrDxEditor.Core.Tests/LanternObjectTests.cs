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

        /// <summary>Capture state is true only for lanterns whose capture attribute parses as true.</summary>
        [Fact]
        public void IsCapturedReadsAttribute()
        {
            Assert.True(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" candyCaptured="true" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" candyCaptured="false" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<lantern x="0" y="0" />""")));
            Assert.False(LanternObject.IsCaptured(Obj("""<candy x="0" y="0" />""")));
        }

        /// <summary>The sprite key switches between idle and active art based on capture state.</summary>
        [Fact]
        public void SpriteKeyReflectsCaptureState()
        {
            Assert.Equal("lantern_active", LanternObject.SpriteKey(Obj("""<lantern x="0" y="0" candyCaptured="true" />""")));
            Assert.Equal("lantern", LanternObject.SpriteKey(Obj("""<lantern x="0" y="0" candyCaptured="false" />""")));
        }

        /// <summary>The level-wide helper detects whether any lantern currently holds the candy.</summary>
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

        /// <summary>Only the first candy in document order is treated as the primary candy.</summary>
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

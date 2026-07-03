using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class CardinalityTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        [Fact]
        public void Target_is_at_capacity_once_one_exists()
        {
            ObjectDescriptor target = DescriptorTable.Default.For("target")!;
            IReadOnlyList<LevelObject> objects = [Obj("""<target x="1" y="2" />""")];

            Assert.True(Cardinality.IsAtCapacity(target, objects));
        }

        [Fact]
        public void Stars_are_never_at_capacity()
        {
            ObjectDescriptor star = DescriptorTable.Default.For("star")!;
            IReadOnlyList<LevelObject> objects =
            [
                Obj("""<star x="1" y="1" />"""),
                Obj("""<star x="2" y="2" />"""),
            ];

            Assert.False(Cardinality.IsAtCapacity(star, objects));
        }
    }
}

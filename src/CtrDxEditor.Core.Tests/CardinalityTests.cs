using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for descriptor placement capacity checks.</summary>
    public class CardinalityTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        /// <summary>Verifies that singleton target descriptors are full once one target exists.</summary>
        [Fact]
        public void TargetIsAtCapacityOnceOneExists()
        {
            ObjectDescriptor target = DescriptorTable.Default.For("target")!;
            IReadOnlyList<LevelObject> objects = [Obj("""<target x="1" y="2" />""")];

            Assert.True(Cardinality.IsAtCapacity(target, objects));
        }

        /// <summary>Verifies that unbounded star descriptors remain placeable with existing stars.</summary>
        [Fact]
        public void StarsAreNeverAtCapacity()
        {
            ObjectDescriptor star = DescriptorTable.Default.For("star")!;
            IReadOnlyList<LevelObject> objects =
            [
                Obj("""<star x="1" y="1" />"""),
                Obj("""<star x="2" y="2" />"""),
            ];

            Assert.False(Cardinality.IsAtCapacity(star, objects));
        }

        /// <summary>Candy and target are unbounded so multi-candy / multi-Om-Nom levels are placeable.</summary>
        [Fact]
        public void CandyAndTargetAreNeverAtCapacity()
        {
            ObjectDescriptor candy = DescriptorTable.Default.For("candy")!;
            ObjectDescriptor target = DescriptorTable.Default.For("target")!;
            IReadOnlyList<LevelObject> objects =
            [
                Obj("""<candy x="1" y="1" />"""),
                Obj("""<target x="2" y="2" />"""),
            ];

            Assert.False(Cardinality.IsAtCapacity(candy, objects));
            Assert.False(Cardinality.IsAtCapacity(target, objects));
        }
    }
}

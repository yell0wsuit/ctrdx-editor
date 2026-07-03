using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class PlacementTests
    {
        [Fact]
        public void CreateObjectSetsCoordinatesAndDefaults()
        {
            ObjectDescriptor star = DescriptorTable.Default.For("star")!;

            LevelObject obj = Placement.CreateObject(star, x: 40, y: 60);

            Assert.Equal("star", obj.Type);
            Assert.Equal(40, obj.X);
            Assert.Equal(60, obj.Y);
            Assert.Equal("-1", obj.GetAttr("timeout"));
        }

        [Fact]
        public void CreateObjectSkipsAttributesWithoutADefault()
        {
            ObjectDescriptor grab = DescriptorTable.Default.For("grab")!;

            LevelObject obj = Placement.CreateObject(grab, x: 1, y: 2);

            Assert.Equal("100", obj.GetAttr("length"));
            Assert.Null(obj.GetAttr("part"));
        }
    }
}

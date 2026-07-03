using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class Vec2Tests
    {
        [Fact]
        public void SubtractionGivesComponentDifference()
        {
            Vec2 result = new Vec2(10, 7) - new Vec2(3, 2);

            Assert.Equal(new Vec2(7, 5), result);
        }
    }
}

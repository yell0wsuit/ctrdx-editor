using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>The game's tutorial path parser tolerates trailing commas and empty components.</summary>
    public class RelativePolylineToleranceTests
    {
        /// <summary>Paths with and without trailing commas parse to the same absolute coordinates.</summary>
        [Theory]
        [InlineData("230,0,440,0")]
        [InlineData("230,0,440,0,")]
        public void TrailingCommaParsesTheSamePoints(string path)
        {
            Vec2[] points = RelativePolyline.Points(new Vec2(10, 20), path);

            Assert.Equal(3, points.Length);
            Assert.Equal(new Vec2(10, 20), points[0]);
            Assert.Equal(new Vec2(240, 20), points[1]);
            Assert.Equal(new Vec2(450, 20), points[2]);
        }

        /// <summary>An empty component reads as zero, matching TutorialMotion.Coordinate.</summary>
        [Fact]
        public void EmptyComponentReadsAsZero()
        {
            Vec2[] points = RelativePolyline.Points(new Vec2(0, 0), "100,,200,0");

            Assert.Equal(3, points.Length);
            Assert.Equal(new Vec2(100, 0), points[1]);
            Assert.Equal(new Vec2(200, 0), points[2]);
        }
    }
}

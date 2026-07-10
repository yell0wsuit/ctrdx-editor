using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for DX mover path parsing, preview, and point serialization.</summary>
    public class MoverPathTests
    {
        /// <summary>Plain DX paths store relative offsets and DX prepends the authored object point.</summary>
        [Fact]
        public void PlainPathPointsIncludeAuthoredStartAndRelativeOffsets()
        {
            Vec2[] points = MoverPath.Points(new Vec2(10, 20), "100,0,100,50");

            Assert.Equal([new Vec2(10, 20), new Vec2(110, 20), new Vec2(110, 70)], points);
        }

        /// <summary>Previewing a plain path follows segment distance and wraps like DX's mover update loop.</summary>
        [Fact]
        public void PreviewPositionFollowsPlainPathSegments()
        {
            Vec2 position = MoverPath.PreviewPosition(new Vec2(10, 20), "100,0,100,50", moveSpeed: 75, elapsedSeconds: 2.0);

            Assert.Equal(110.0, position.X, 6);
            Assert.Equal(70.0, position.Y, 6);
        }

        /// <summary>Looped serialization writes just the authored shape points relative to the object.</summary>
        [Fact]
        public void SerializePlainLoopWritesDrawnPointsOnly()
        {
            string path = MoverPath.SerializePlain(
                new Vec2(10, 20),
                [new Vec2(10, 20), new Vec2(110, 20), new Vec2(110, 70)],
                loop: true);

            Assert.Equal("100,0,100,50", path);
        }

        /// <summary>Non-loop serialization mirrors the path back through intermediate points before DX wraps home.</summary>
        [Fact]
        public void SerializePlainNoLoopWritesPingPongReturnPoints()
        {
            string path = MoverPath.SerializePlain(
                new Vec2(10, 20),
                [new Vec2(10, 20), new Vec2(110, 20), new Vec2(110, 70), new Vec2(10, 70)],
                loop: false);

            Assert.Equal("100,0,100,50,0,50,100,50,100,0", path);
        }

        /// <summary>Generated plain paths are capped to DX's non-R capacity of 100 total points including start.</summary>
        [Fact]
        public void SerializePlainCapsStoredOffsetsToDxCapacity()
        {
            Vec2[] points = new Vec2[150];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vec2(i, 0);
            }

            string path = MoverPath.SerializePlain(new Vec2(0, 0), points, loop: true);

            Assert.Equal(MoverPath.MaxStoredPlainOffsetPoints * 2, path.Split(',').Length);
            Assert.EndsWith("99,0", path);
        }
    }
}

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

        /// <summary>Retrace paths expose only their outbound canonical points.</summary>
        [Fact]
        public void CanonicalPointsReturnsOutboundHalfForRetrace()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50,0,50,100,50,100,0";

            Assert.True(MoverPath.IsRetrace(path));
            Assert.Equal([start, new Vec2(100, 0), new Vec2(100, 50), new Vec2(0, 50)],
                MoverPath.CanonicalPoints(start, path));
        }

        /// <summary>Moving and inserting canonical points preserve the path's retrace state.</summary>
        [Fact]
        public void CanonicalMoveAndInsertPreserveRetrace()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50,0,50,100,50,100,0";

            string moved = MoverPath.MoveCanonicalPoint(start, path, 2, new Vec2(125, 50));
            string inserted = MoverPath.InsertCanonicalPoint(start, moved, 1, new Vec2(110, 25));

            Assert.True(MoverPath.IsRetrace(inserted));
            Assert.Equal(
                [start, new Vec2(100, 0), new Vec2(110, 25), new Vec2(125, 50), new Vec2(0, 50)],
                MoverPath.CanonicalPoints(start, inserted));
        }

        /// <summary>Appending adds a new canonical endpoint and hit-testing skips the object point.</summary>
        [Fact]
        public void AppendAndHitCanonicalPointUseEditableWaypoints()
        {
            Vec2 start = new(0, 0);
            string edited = MoverPath.AppendCanonicalPoint(start, "100,0", new Vec2(100, 50));

            Assert.Equal([start, new Vec2(100, 0), new Vec2(100, 50)],
                MoverPath.CanonicalPoints(start, edited));
            Assert.Equal(2, MoverPath.HitCanonicalPoint(start, edited, new Vec2(101, 49), tolerance: 5));
            Assert.Equal(-1, MoverPath.HitCanonicalPoint(start, edited, new Vec2(1, 1), tolerance: 5));
        }

        /// <summary>Deleting an interior canonical waypoint reconnects its neighbors on a circuit.</summary>
        [Fact]
        public void DeleteCanonicalPointRemovesInteriorVertexOnCircuit()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50,0,50";

            string edited = MoverPath.DeleteCanonicalPoint(start, path, 2);

            Assert.Equal([start, new Vec2(100, 0), new Vec2(0, 50)],
                MoverPath.CanonicalPoints(start, edited));
        }

        /// <summary>Deleting a canonical waypoint on a retrace keeps the path a retrace.</summary>
        [Fact]
        public void DeleteCanonicalPointPreservesRetrace()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50,0,50,100,50,100,0";

            string edited = MoverPath.DeleteCanonicalPoint(start, path, 2);

            Assert.True(MoverPath.IsRetrace(edited));
            Assert.Equal([start, new Vec2(100, 0), new Vec2(0, 50)],
                MoverPath.CanonicalPoints(start, edited));
        }

        /// <summary>Deleting the object point (index 0) or an out-of-range index is a no-op.</summary>
        [Fact]
        public void DeleteCanonicalPointIgnoresObjectPointAndOutOfRange()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50";

            Assert.Equal(path, MoverPath.DeleteCanonicalPoint(start, path, 0));
            Assert.Equal(path, MoverPath.DeleteCanonicalPoint(start, path, 9));
        }

        /// <summary>SetRetrace(true) expands the outbound shape to an out-and-back palindrome.</summary>
        [Fact]
        public void SetRetraceTrueProducesRetrace()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50";

            string edited = MoverPath.SetRetrace(start, path, retrace: true);

            Assert.True(MoverPath.IsRetrace(edited));
            Assert.Equal([start, new Vec2(100, 0), new Vec2(100, 50)],
                MoverPath.CanonicalPoints(start, edited));
        }

        /// <summary>SetRetrace(false) collapses a retrace back to a plain circuit of its outbound points.</summary>
        [Fact]
        public void SetRetraceFalseProducesCircuit()
        {
            Vec2 start = new(0, 0);
            string path = "100,0,100,50,0,50,100,50,100,0";

            string edited = MoverPath.SetRetrace(start, path, retrace: false);

            Assert.False(MoverPath.IsRetrace(edited));
            Assert.Equal("100,0,100,50,0,50", edited);
        }

        /// <summary>SetRetrace leaves circular and empty paths untouched.</summary>
        [Fact]
        public void SetRetraceIgnoresCircularAndEmpty()
        {
            Assert.Equal("RC120", MoverPath.SetRetrace(new Vec2(0, 0), "RC120", true));
            Assert.Equal(string.Empty, MoverPath.SetRetrace(new Vec2(0, 0), "", true));
        }
    }
}

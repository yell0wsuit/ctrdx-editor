using System.Linq;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for format-neutral relative polyline parsing and editing.</summary>
    public class RelativePolylineTests
    {
        /// <summary>Only complete coordinate pairs are parsed, and a trailing separator is harmless.</summary>
        [Fact]
        public void ParseOffsetsIgnoresTrailingCommaAndOddValue()
        {
            Assert.Equal(
                [new Vec2(10, 20), new Vec2(40, 15), new Vec2(70, 30)],
                RelativePolyline.Points(new Vec2(10, 20), "30,-5,60,10,999,"));
        }

        /// <summary>Visual edits normalize malformed tails and serialize every vertex relative to the anchor.</summary>
        [Fact]
        public void EditOperationsNormalizeAndSerializeRelativeToAnchor()
        {
            Vec2 start = new(10, 20);

            string moved = RelativePolyline.MovePoint(start, "30,0,60,0,odd", 1, new Vec2(50, 40));
            string inserted = RelativePolyline.InsertPoint(start, moved, 1, new Vec2(60, 30));

            Assert.Equal("40,20,50,10,60,0", inserted);
        }

        /// <summary>Append and delete operate on stored points while protecting the anchor.</summary>
        [Fact]
        public void AppendAndDeleteKeepAnchorAndStoredPointOrder()
        {
            Vec2 start = new(10, 20);

            string appended = RelativePolyline.AppendPoint(start, "30,0", new Vec2(70, 40));
            string deleted = RelativePolyline.DeletePoint(start, appended, 1);

            Assert.Equal("60,20", deleted);
            Assert.Equal(appended, RelativePolyline.DeletePoint(start, appended, 0));
        }

        /// <summary>Deletion respects the caller's minimum total point count.</summary>
        [Fact]
        public void DeleteRefusesToGoBelowMinimumPointCount()
        {
            Vec2 start = new(0, 0);

            Assert.Equal("100,0", RelativePolyline.DeletePoint(start, "100,0", 1, minimumPointCount: 2));
            Assert.Equal(string.Empty, RelativePolyline.DeletePoint(start, "100,0", 1));
        }

        /// <summary>Hit testing starts at the configured editable index and uses the supplied tolerance.</summary>
        [Fact]
        public void HitPointSkipsProtectedPoints()
        {
            Vec2 start = new(10, 20);

            Assert.Equal(2, RelativePolyline.HitPoint(start, "30,0,60,20", new Vec2(71, 39), 2));
            Assert.Equal(-1, RelativePolyline.HitPoint(start, "30,0", start, 2));
            Assert.Equal(0, RelativePolyline.HitPoint(start, "30,0", start, 2, firstEditableIndex: 0));
        }

        /// <summary>Serialization uses invariant whole and round-trip values and preserves duplicates.</summary>
        [Fact]
        public void SerializeUsesInvariantRoundTripFormattingAndPreservesDuplicates()
        {
            Vec2 start = new(10, 20);
            Vec2 fractional = new(10.123456789012345, 21.5);

            string serialized = RelativePolyline.Serialize(start, [start, fractional, fractional]);

            Assert.Equal("0.12345678901234436,1.5,0.12345678901234436,1.5", serialized);
        }

        /// <summary>Writes never exceed the game format's stored-point capacity.</summary>
        [Fact]
        public void WritesEnforceMaximumStoredPointCount()
        {
            Vec2 start = new(0, 0);
            Vec2[] points = [.. Enumerable.Range(0, 150).Select(i => new Vec2(i, 0))];
            string full = RelativePolyline.Serialize(start, points);

            Assert.Equal(RelativePolyline.MaxStoredOffsetPoints * 2, full.Split(',').Length);
            Assert.False(RelativePolyline.CanAddPoint(start, full));
            Assert.Equal(full, RelativePolyline.AppendPoint(start, full, new Vec2(200, 0)));
            Assert.Equal(full, RelativePolyline.InsertPoint(start, full, 0, new Vec2(0.5, 0)));
        }
    }
}

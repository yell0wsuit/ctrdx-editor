using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the object rotation dial geometry (pure, UI-free).</summary>
    public class ObjectRotationTests
    {
        // The pump spec exercises a non-zero display offset (+90); a plain spec covers the zero-offset case.
        private static readonly RotationSpec PumpSpec = new(DisplayOffset: 90);
        private static readonly RotationSpec ZeroSpec = new(DisplayOffset: 0);

        private static LevelObject Obj(string? angle, string attr = "angle")
        {
            XElement e = new("pump");
            if (angle is not null)
            {
                e.SetAttributeValue(attr, angle);
            }
            return new LevelObject(e);
        }

        /// <summary>Verifies the stored angle parses invariant floats and defaults to zero when absent.</summary>
        [Fact]
        public void StoredAngleParsesFloatAndDefaultsToZero()
        {
            Assert.Equal(45, ObjectRotation.StoredAngle(Obj("45"), PumpSpec));
            Assert.Equal(-120, ObjectRotation.StoredAngle(Obj("-120"), PumpSpec));
            Assert.Equal(0, ObjectRotation.StoredAngle(Obj(null), PumpSpec));
        }

        /// <summary>Verifies the stored angle reads the attribute named by the spec, not always "angle".</summary>
        [Fact]
        public void StoredAngleHonoursTheSpecAttributeName()
        {
            RotationSpec spec = new(DisplayOffset: 0, AttributeName: "rot");
            Assert.Equal(33, ObjectRotation.StoredAngle(Obj("33", attr: "rot"), spec));
        }

        /// <summary>Verifies the on-screen rotation adds the spec's display offset to the stored angle.</summary>
        [Fact]
        public void DisplayDegreesAddsTheOffset()
        {
            Assert.Equal(90, ObjectRotation.DisplayDegrees(Obj("0"), PumpSpec));
            Assert.Equal(360, ObjectRotation.DisplayDegrees(Obj("270"), PumpSpec));
            Assert.Equal(45, ObjectRotation.DisplayDegrees(Obj("45"), ZeroSpec));
        }

        /// <summary>Verifies angles wrap into the signed half-turn range (-180, 180].</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(180, 180)]
        [InlineData(181, -179)]
        [InlineData(270, -90)]
        [InlineData(-270, 90)]
        [InlineData(360, 0)]
        public void NormalizeWrapsIntoSignedHalfTurn(double input, double expected)
        {
            Assert.Equal(expected, ObjectRotation.Normalize(input));
        }

        /// <summary>Verifies snapping rounds to the nearest step (15°), away from zero at the midpoint.</summary>
        [Theory]
        [InlineData(7, 0)]
        [InlineData(8, 15)]
        [InlineData(22, 15)]
        [InlineData(23, 30)]
        [InlineData(-8, -15)]
        public void SnapRoundsToNearestStep(double input, double expected)
        {
            Assert.Equal(expected, ObjectRotation.Snap(input, 15));
        }

        /// <summary>Verifies a snapped drag around a +90-offset center yields the cardinal stored angles.</summary>
        [Fact]
        public void AngleFromPointSnappedGivesCardinalStoredAngles()
        {
            Vec2 c = new(100, 100);
            // Point directly below the center (level Y is down): display dir = +90, stored = 0.
            Assert.Equal(0, ObjectRotation.AngleFromPoint(c, new Vec2(100, 200), PumpSpec, snap: true));
            // Point directly right: display dir = 0, stored = -90.
            Assert.Equal(-90, ObjectRotation.AngleFromPoint(c, new Vec2(200, 100), PumpSpec, snap: true));
            // Point directly above: display dir = -90, stored = -180 -> normalized 180.
            Assert.Equal(180, ObjectRotation.AngleFromPoint(c, new Vec2(100, 0), PumpSpec, snap: true));
        }

        /// <summary>Verifies a zero-offset spec maps a rightward drag to stored angle 0.</summary>
        [Fact]
        public void AngleFromPointRespectsZeroOffsetSpec()
        {
            Vec2 c = new(0, 0);
            // Zero offset: point directly right -> display dir 0 -> stored 0.
            Assert.Equal(0, ObjectRotation.AngleFromPoint(c, new Vec2(100, 0), ZeroSpec, snap: true));
        }

        /// <summary>Verifies a free (unsnapped) drag still produces whole-degree angles.</summary>
        [Fact]
        public void AngleFromPointFreeRoundsToWholeDegrees()
        {
            Vec2 c = new(0, 0);
            double a = ObjectRotation.AngleFromPoint(c, new Vec2(100, 3), PumpSpec, snap: false);
            Assert.Equal(System.Math.Round(a), a); // integer degrees
        }

        /// <summary>Verifies the knob sits along the object's display direction at the ring radius.</summary>
        [Fact]
        public void KnobPositionMatchesDisplayDirection()
        {
            Vec2 c = new(0, 0);
            // stored 0, offset 90 -> display 90 -> knob straight down at radius.
            Vec2 knob = ObjectRotation.KnobPosition(c, storedAngle: 0, PumpSpec, radius: 50);
            Assert.Equal(0, knob.X, 3);
            Assert.Equal(50, knob.Y, 3);
        }

        /// <summary>Verifies ring hit-testing accepts points near the edge and rejects interior points.</summary>
        [Fact]
        public void OnRingDetectsPointsNearTheCircleEdge()
        {
            Vec2 c = new(0, 0);
            Assert.True(ObjectRotation.OnRing(c, radius: 50, new Vec2(52, 0), tolerance: 5));
            Assert.False(ObjectRotation.OnRing(c, radius: 50, new Vec2(20, 0), tolerance: 5));
        }

        /// <summary>Verifies hit classification prefers the knob, then the ring, then none.</summary>
        [Fact]
        public void HitTestPrefersKnobThenRingThenNone()
        {
            Vec2 c = new(0, 0);
            Vec2 knob = ObjectRotation.KnobPosition(c, storedAngle: 0, PumpSpec, radius: 50);
            Assert.Equal(ObjectRotation.Handle.Knob,
                ObjectRotation.HitTest(c, 0, PumpSpec, 50, knob, ringTolerance: 5, knobTolerance: 6));
            // A point on the ring but away from the knob classifies as Ring.
            Assert.Equal(ObjectRotation.Handle.Ring,
                ObjectRotation.HitTest(c, 0, PumpSpec, 50, new Vec2(0, -50), ringTolerance: 5, knobTolerance: 6));
            // Well inside the ring: nothing.
            Assert.Equal(ObjectRotation.Handle.None,
                ObjectRotation.HitTest(c, 0, PumpSpec, 50, new Vec2(0, 0), ringTolerance: 5, knobTolerance: 6));
        }

        /// <summary>Verifies dial-produced angles format as invariant whole degrees.</summary>
        [Fact]
        public void FormatWritesInvariantIntegerDegrees()
        {
            Assert.Equal("15", ObjectRotation.Format(15));
            Assert.Equal("-90", ObjectRotation.Format(-90));
        }

        /// <summary>Verifies the rotation table exposes the pump spec and treats non-rotatable objects as null.</summary>
        [Fact]
        public void RotationTableKnowsPumpAndNotOthers()
        {
            Assert.True(RotationTable.IsRotatable("pump"));
            Assert.Equal(90, RotationTable.For("pump")!.DisplayOffset);
            Assert.Equal("angle", RotationTable.For("pump")!.AttributeName);
            Assert.True(RotationTable.IsRotatable("electro"));
            Assert.Equal(0, RotationTable.For("electro")!.DisplayOffset);
            Assert.Null(RotationTable.For("grab"));
            Assert.False(RotationTable.IsRotatable("star"));
        }

        /// <summary>DX's sock art has a fixed 90-degree turn but no editable angle attribute.</summary>
        [Theory]
        [InlineData("sock")]
        [InlineData("sock_grouped")]
        [InlineData("sock_xmas")]
        [InlineData("sock_xmas_grouped")]
        public void SockVisualKeysHaveFixedNonEditableRotation(string key)
        {
            RotationSpec spec = RotationTable.For(key)!;

            Assert.Equal(90, spec.DisplayOffset);
            Assert.False(spec.Editable);
            Assert.False(RotationTable.IsRotatable(key));
            Assert.Null(RotationTable.EditableFor(key));
        }
    }
}

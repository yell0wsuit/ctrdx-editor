using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the pump rotation dial geometry (pure, UI-free).</summary>
    public class PumpRotationTests
    {
        private static LevelObject Pump(string? angle)
        {
            XElement e = new("pump");
            if (angle is not null)
            {
                e.SetAttributeValue("angle", angle);
            }
            return new LevelObject(e);
        }

        [Fact]
        public void StoredAngleParsesFloatAndDefaultsToZero()
        {
            Assert.Equal(45, PumpRotation.StoredAngle(Pump("45")));
            Assert.Equal(-120, PumpRotation.StoredAngle(Pump("-120")));
            Assert.Equal(0, PumpRotation.StoredAngle(Pump(null)));
        }

        [Fact]
        public void DisplayDegreesAddsNinety()
        {
            Assert.Equal(90, PumpRotation.DisplayDegrees(Pump("0")));
            Assert.Equal(360, PumpRotation.DisplayDegrees(Pump("270")));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(180, 180)]
        [InlineData(181, -179)]
        [InlineData(270, -90)]
        [InlineData(-270, 90)]
        [InlineData(360, 0)]
        public void NormalizeWrapsIntoSignedHalfTurn(double input, double expected)
        {
            Assert.Equal(expected, PumpRotation.Normalize(input));
        }

        [Theory]
        [InlineData(7, 0)]
        [InlineData(8, 15)]
        [InlineData(22, 15)]
        [InlineData(23, 30)]
        [InlineData(-8, -15)]
        public void SnapRoundsToNearestFifteen(double input, double expected)
        {
            Assert.Equal(expected, PumpRotation.Snap(input));
        }

        [Fact]
        public void AngleFromPointSnappedGivesCardinalStoredAngles()
        {
            Vec2 c = new(100, 100);
            // Point directly below the center (level Y is down): display dir = +90, stored = 0.
            Assert.Equal(0, PumpRotation.AngleFromPoint(c, new Vec2(100, 200), snap: true));
            // Point directly right: display dir = 0, stored = -90.
            Assert.Equal(-90, PumpRotation.AngleFromPoint(c, new Vec2(200, 100), snap: true));
            // Point directly above: display dir = -90, stored = -180 -> normalized 180.
            Assert.Equal(180, PumpRotation.AngleFromPoint(c, new Vec2(100, 0), snap: true));
        }

        [Fact]
        public void AngleFromPointFreeRoundsToWholeDegrees()
        {
            Vec2 c = new(0, 0);
            double a = PumpRotation.AngleFromPoint(c, new Vec2(100, 3), snap: false);
            Assert.Equal(System.Math.Round(a), a); // integer degrees
        }

        [Fact]
        public void KnobPositionMatchesDisplayDirection()
        {
            Vec2 c = new(0, 0);
            // stored 0 -> display 90 -> knob straight down at radius.
            Vec2 knob = PumpRotation.KnobPosition(c, storedAngle: 0, radius: 50);
            Assert.Equal(0, knob.X, 3);
            Assert.Equal(50, knob.Y, 3);
        }

        [Fact]
        public void OnRingDetectsPointsNearTheCircleEdge()
        {
            Vec2 c = new(0, 0);
            Assert.True(PumpRotation.OnRing(c, radius: 50, new Vec2(52, 0), tolerance: 5));
            Assert.False(PumpRotation.OnRing(c, radius: 50, new Vec2(20, 0), tolerance: 5));
        }

        [Fact]
        public void OnKnobDetectsPointsNearTheKnob()
        {
            Vec2 c = new(0, 0);
            Vec2 knob = PumpRotation.KnobPosition(c, storedAngle: 0, radius: 50);
            Assert.True(PumpRotation.OnKnob(c, 0, 50, knob, tolerance: 6));
            Assert.False(PumpRotation.OnKnob(c, 0, 50, new Vec2(0, -50), tolerance: 6));
        }

        [Fact]
        public void FormatWritesInvariantIntegerDegrees()
        {
            Assert.Equal("15", PumpRotation.Format(15));
            Assert.Equal("-90", PumpRotation.Format(-90));
        }
    }
}

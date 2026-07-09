using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the mover-backed object spin model.</summary>
    public class ObjectSpinTests
    {
        private static LevelObject Obj(string type, string? rotateSpeed = null, string? path = null, string? moveSpeed = null)
        {
            XElement e = new(type);
            if (rotateSpeed is not null)
            {
                e.SetAttributeValue("rotateSpeed", rotateSpeed);
            }
            if (path is not null)
            {
                e.SetAttributeValue("path", path);
            }
            if (moveSpeed is not null)
            {
                e.SetAttributeValue("moveSpeed", moveSpeed);
            }
            return new LevelObject(e);
        }

        /// <summary>Spin opt-in follows the object registry, parallel to RotationTable.</summary>
        [Fact]
        public void SpinTableKnowsStarsAndSpikes()
        {
            Assert.True(SpinTable.IsSpinnable("star"));
            Assert.True(SpinTable.IsSpinnable("spike1"));
            Assert.True(SpinTable.IsSpinnable("spike4"));
            Assert.False(SpinTable.IsSpinnable("grab"));
        }

        /// <summary>Missing, zero, and invalid rotateSpeed values are treated as no active spin.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("not-a-number")]
        public void ActiveSpinRequiresNonZeroRotateSpeed(string? rotateSpeed)
        {
            Assert.False(ObjectSpin.IsSpinning(Obj("star", rotateSpeed)));
        }

        /// <summary>Speed is exposed as a positive whole-number magnitude, hiding XML sign from the user.</summary>
        [Theory]
        [InlineData("70", 70)]
        [InlineData("-130", 130)]
        [InlineData("12.9", 12)]
        public void SpeedMagnitudeUsesAbsoluteTruncatedWholeNumber(string rotateSpeed, int expected)
        {
            Assert.True(ObjectSpin.IsSpinning(Obj("star", rotateSpeed)));
            Assert.Equal(expected, ObjectSpin.SpinSpeed(Obj("star", rotateSpeed)));
        }

        /// <summary>The clockwise checkbox maps directly to positive rotateSpeed values.</summary>
        [Theory]
        [InlineData("70", true)]
        [InlineData("-130", false)]
        public void ClockwiseFollowsRotateSpeedSign(string rotateSpeed, bool expected)
        {
            Assert.Equal(expected, ObjectSpin.SpinClockwise(Obj("star", rotateSpeed)));
        }

        /// <summary>Writing spin stores a signed whole-number rotateSpeed and disabling removes the attribute.</summary>
        [Fact]
        public void SetSpinWritesSignedSpeedAndDisableRemovesAttribute()
        {
            LevelObject star = Obj("star");

            ObjectSpin.SetSpin(star, enabled: true, speed: 70, clockwise: false);

            Assert.Equal("-70", star.GetAttr("rotateSpeed"));

            ObjectSpin.SetSpin(star, enabled: true, speed: 0, clockwise: true);

            Assert.Null(star.GetAttr("rotateSpeed"));
        }

        /// <summary>Writing spin creates the minimal static path DX needs to construct a rotating mover.</summary>
        [Fact]
        public void SetSpinAddsStaticPathWithoutOverwritingAuthoredPath()
        {
            LevelObject star = Obj("star");
            LevelObject movingSpike = Obj("spike2", path: "10,0,10,10");

            ObjectSpin.SetSpin(star, enabled: true, speed: 70, clockwise: true);
            ObjectSpin.SetSpin(movingSpike, enabled: true, speed: 130, clockwise: false);

            Assert.Equal("0,0", star.GetAttr("path"));
            Assert.Equal("10,0,10,10", movingSpike.GetAttr("path"));
        }

        /// <summary>Orbital movement follows DX circular path syntax and defaults movement speed separately.</summary>
        [Fact]
        public void SetOrbitalWritesCircularPathRadiusWithoutClearingRotateSpeed()
        {
            LevelObject star = Obj("star", rotateSpeed: "70");

            ObjectSpin.SetOrbital(star, enabled: true, radius: 45, clockwise: false);

            Assert.Equal("RW45", star.GetAttr("path"));
            Assert.Equal("70", star.GetAttr("moveSpeed"));
            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            Assert.True(ObjectSpin.IsSpinning(star));
            Assert.True(ObjectSpin.IsOrbital(star));
            Assert.Equal(45, ObjectSpin.OrbitRadius(star));
            Assert.False(ObjectSpin.OrbitClockwise(star));
        }

        /// <summary>Disabling orbital movement removes circular mover attributes without clearing self-spin.</summary>
        [Fact]
        public void DisablingOrbitalRemovesCircularPathAndMoveSpeedWithoutClearingRotateSpeed()
        {
            LevelObject star = Obj("star", rotateSpeed: "-70", path: "RC40", moveSpeed: "70");

            ObjectSpin.SetOrbital(star, enabled: false, radius: 70, clockwise: true);

            Assert.Null(star.GetAttr("path"));
            Assert.Null(star.GetAttr("moveSpeed"));
            Assert.Equal("-70", star.GetAttr("rotateSpeed"));
            Assert.True(ObjectSpin.IsSpinning(star));
        }

        /// <summary>Orbit-only data is active without rotateSpeed.</summary>
        [Fact]
        public void OrbitDoesNotCountAsSelfSpin()
        {
            LevelObject star = Obj("star", path: "RC40", moveSpeed: "70");

            Assert.False(ObjectSpin.IsSpinning(star));
            Assert.True(ObjectSpin.IsOrbital(star));
        }

        /// <summary>Orbital movement speed is preserved when switching direction or radius.</summary>
        [Fact]
        public void SetOrbitalSpinPreservesAuthoredMoveSpeed()
        {
            LevelObject star = Obj("star", path: "RW60", moveSpeed: "80");

            ObjectSpin.SetOrbital(star, enabled: true, radius: 40, clockwise: true);

            Assert.Equal("RC40", star.GetAttr("path"));
            Assert.Equal("80", star.GetAttr("moveSpeed"));
            Assert.Equal(40, ObjectSpin.OrbitRadius(star));
        }

        /// <summary>Orbit speed exposes DX moveSpeed as a positive whole-number movement speed.</summary>
        [Theory]
        [InlineData("80", 80)]
        [InlineData("-90", 90)]
        [InlineData("12.9", 12)]
        [InlineData("not-a-number", 0)]
        public void OrbitSpeedUsesMoveSpeedMagnitude(string moveSpeed, int expected)
        {
            Assert.Equal(expected, ObjectSpin.OrbitSpeed(Obj("star", path: "RC40", moveSpeed: moveSpeed)));
        }

        /// <summary>Writing orbit speed updates moveSpeed without changing the circular path or rotateSpeed.</summary>
        [Fact]
        public void SetOrbitSpeedWritesMoveSpeedOnly()
        {
            LevelObject star = Obj("star", rotateSpeed: "-70", path: "RW60", moveSpeed: "80");

            ObjectSpin.SetOrbitSpeed(star, speed: 120);

            Assert.Equal("120", star.GetAttr("moveSpeed"));
            Assert.Equal("RW60", star.GetAttr("path"));
            Assert.Equal("-70", star.GetAttr("rotateSpeed"));
        }

        /// <summary>Writing self-spin preserves existing circular orbit data.</summary>
        [Fact]
        public void SetSpinPreservesCircularOrbit()
        {
            LevelObject star = Obj("star", path: "RW60", moveSpeed: "80");

            ObjectSpin.SetSpin(star, enabled: true, speed: 130, clockwise: false);

            Assert.Equal("-130", star.GetAttr("rotateSpeed"));
            Assert.Equal("RW60", star.GetAttr("path"));
            Assert.Equal("80", star.GetAttr("moveSpeed"));
        }

        /// <summary>Live preview rotation advances by signed rotateSpeed degrees per elapsed second.</summary>
        [Theory]
        [InlineData("70", 0.5, 35.0)]
        [InlineData("-130", 2.0, -260.0)]
        [InlineData("0", 10.0, 0.0)]
        [InlineData("12.9", 1.0, 12.0)]
        public void PreviewDegreesUsesSignedWholeSpeed(string rotateSpeed, double elapsedSeconds, double expected)
        {
            Assert.Equal(expected, ObjectSpin.PreviewDegrees(Obj("star", rotateSpeed), elapsedSeconds));
        }

        /// <summary>Orbital preview starts at the first generated DX circular path point.</summary>
        [Fact]
        public void PreviewPositionStartsAtFirstCircularPathPoint()
        {
            Vec2 preview = ObjectSpin.PreviewPosition(Obj("star", path: "RC8", moveSpeed: "10"), 0.0);

            Assert.Equal(8.0, preview.X);
            Assert.Equal(0.0, preview.Y, precision: 6);
        }

        /// <summary>Orbital preview advances along RC and RW generated path points using moveSpeed.</summary>
        [Fact]
        public void PreviewPositionUsesMoveSpeedAndOrbitDirection()
        {
            Vec2 clockwise = ObjectSpin.PreviewPosition(Obj("star", path: "RC8", moveSpeed: "8"), 1.0);
            Vec2 counterClockwise = ObjectSpin.PreviewPosition(Obj("star", path: "RW8", moveSpeed: "8"), 1.0);

            Assert.Equal(2.3431457505, clockwise.X, precision: 6);
            Assert.Equal(5.6568542495, clockwise.Y, precision: 6);
            Assert.Equal(2.3431457505, counterClockwise.X, precision: 6);
            Assert.Equal(-5.6568542495, counterClockwise.Y, precision: 6);
        }

        /// <summary>Objects without orbital data preview at their authored position.</summary>
        [Fact]
        public void PreviewPositionFallsBackToAuthoredPositionWithoutOrbit()
        {
            Vec2 preview = ObjectSpin.PreviewPosition(Obj("star", rotateSpeed: "70"), 2.0);

            Assert.Equal(new Vec2(0.0, 0.0), preview);
        }
    }
}

using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the rotateSpeed-backed object spin model.</summary>
    public class ObjectSpinTests
    {
        private static LevelObject Obj(string type, string? rotateSpeed = null, string? path = null)
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
            Assert.Equal(expected, ObjectSpin.Speed(Obj("star", rotateSpeed)));
        }

        /// <summary>The clockwise checkbox maps directly to positive rotateSpeed values.</summary>
        [Theory]
        [InlineData("70", true)]
        [InlineData("-130", false)]
        public void ClockwiseFollowsRotateSpeedSign(string rotateSpeed, bool expected)
        {
            Assert.Equal(expected, ObjectSpin.Clockwise(Obj("star", rotateSpeed)));
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
    }
}

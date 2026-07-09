using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the rotateSpeed-backed object spin model.</summary>
    public class ObjectSpinTests
    {
        private static LevelObject Obj(string type, string? rotateSpeed = null)
        {
            XElement e = new(type);
            if (rotateSpeed is not null)
            {
                e.SetAttributeValue("rotateSpeed", rotateSpeed);
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
    }
}

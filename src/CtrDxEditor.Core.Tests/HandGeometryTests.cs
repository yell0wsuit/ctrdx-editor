using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests mechanical hand chain geometry against the game's JointAtIndexPosition.</summary>
    public class HandGeometryTests
    {
        internal static LevelObject Hand(int x, int y, params (double Angle, double Length, bool Rotatable)[] segments)
        {
            XElement e = new("hand");
            e.SetAttributeValue("x", x);
            e.SetAttributeValue("y", y);
            e.SetAttributeValue("segmentsCount", segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                (double angle, double length, bool rotatable) = segments[i];
                e.SetAttributeValue($"segment{i + 1}Angle", angle);
                e.SetAttributeValue($"segment{i + 1}Length", length);
                e.SetAttributeValue($"segment{i + 1}Rotatable", rotatable ? "true" : "false");
            }
            return new LevelObject(e);
        }

        /// <summary>The segment spec targets the indexed angle attribute with a 90-degree snap and no offset.</summary>
        [Fact]
        public void SegmentSpecTargetsIndexedAngleAttribute()
        {
            RotationSpec spec = HandGeometry.SegmentSpec(2);
            Assert.Equal("segment2Angle", spec.AttributeName);
            Assert.Equal(0, spec.DisplayOffset);
            Assert.Equal(90, spec.SnapStep);
            Assert.Equal(1, spec.StoredAngleSign);
        }

        /// <summary>Joint 0 is the hand's own anchor.</summary>
        [Fact]
        public void JointZeroIsTheBase()
        {
            LevelObject hand = Hand(162, 254, (-90, 70, true));
            Assert.Equal(new Vec2(162, 254), HandGeometry.Joint(hand, 0));
        }

        /// <summary>Angle -90 points up the y-down screen, matching the authored levels.</summary>
        [Fact]
        public void NegativeNinetyPointsUp()
        {
            LevelObject hand = Hand(162, 254, (-90, 70, true));
            Vec2 tip = HandGeometry.Joint(hand, 1);
            Assert.Equal(162, tip.X, 6);
            Assert.Equal(184, tip.Y, 6);
        }

        /// <summary>
        /// Angles are absolute, so a two-segment chain accumulates positions but not angles. This mirrors
        /// the 6_14.xml hand: both segments at -90 stack straight up.
        /// </summary>
        [Fact]
        public void AbsoluteAnglesStackWithoutAccumulating()
        {
            LevelObject hand = Hand(162, 254, (-90, 70, true), (-90, 70, true));
            Vec2[] joints = HandGeometry.Joints(hand);

            Assert.Equal(3, joints.Length);
            Assert.Equal(new Vec2(162, 254), joints[0]);
            Assert.Equal(184, joints[1].Y, 6);
            Assert.Equal(114, joints[2].Y, 6);
            Assert.Equal(162, joints[2].X, 6);
        }

        /// <summary>A right-angle bend resolves from absolute angles alone.</summary>
        [Fact]
        public void RightAngleBendResolvesFromAbsoluteAngles()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true), (90, 40, false));
            Vec2[] joints = HandGeometry.Joints(hand);

            Assert.Equal(150, joints[1].X, 6);
            Assert.Equal(200, joints[1].Y, 6);
            Assert.Equal(150, joints[2].X, 6);
            Assert.Equal(240, joints[2].Y, 6);
        }

        /// <summary>The claw sits on the last joint; the runtime candy-anchor offset is not modeled.</summary>
        [Fact]
        public void ClawSitsOnLastJoint()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true), (90, 40, false));
            Assert.Equal(HandGeometry.Joint(hand, 2), HandGeometry.ClawPosition(hand));
        }

        /// <summary>A hand with no live segments collapses to its base.</summary>
        [Fact]
        public void ZeroSegmentHandCollapsesToBase()
        {
            LevelObject hand = Hand(100, 200);
            _ = Assert.Single(HandGeometry.Joints(hand));
            Assert.Equal(new Vec2(100, 200), HandGeometry.ClawPosition(hand));
        }

        /// <summary>Dead slots past segmentsCount never contribute to the chain.</summary>
        [Fact]
        public void DeadSlotsDoNotExtendTheChain()
        {
            LevelObject hand = Hand(162, 254, (-90, 70, true), (-90, 70, true));
            hand.SetAttr("segment3Angle", "-90");
            hand.SetAttr("segment3Length", "70");

            Assert.Equal(3, HandGeometry.Joints(hand).Length);
            Assert.Equal(114, HandGeometry.ClawPosition(hand).Y, 6);
        }

        /// <summary>Bounds enclose every joint.</summary>
        [Fact]
        public void BoundsEncloseAllJoints()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true), (90, 40, false));
            LevelBounds b = HandGeometry.Bounds(hand);

            Assert.True(b.X <= 100);
            Assert.True(b.Y <= 200);
            Assert.True(b.X + b.W >= 150);
            Assert.True(b.Y + b.H >= 240);
        }
    }
}

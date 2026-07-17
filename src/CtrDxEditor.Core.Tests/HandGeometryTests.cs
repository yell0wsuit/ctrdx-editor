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
            Assert.Equal(15, spec.SnapStep);
            Assert.Equal(1, spec.StoredAngleSign);
        }

        /// <summary>Joint length cursors follow the segment's nearest screen axis.</summary>
        [Theory]
        [InlineData(0, HandGeometry.ResizeAxis.Horizontal)]
        [InlineData(45, HandGeometry.ResizeAxis.Horizontal)]
        [InlineData(46, HandGeometry.ResizeAxis.Vertical)]
        [InlineData(90, HandGeometry.ResizeAxis.Vertical)]
        [InlineData(134, HandGeometry.ResizeAxis.Vertical)]
        [InlineData(135, HandGeometry.ResizeAxis.Horizontal)]
        [InlineData(-90, HandGeometry.ResizeAxis.Vertical)]
        [InlineData(180, HandGeometry.ResizeAxis.Horizontal)]
        public void SegmentResizeAxisUsesNearestCardinalAxis(double angle, HandGeometry.ResizeAxis expected)
        {
            LevelObject hand = Hand(100, 200, (angle, 50, true));

            Assert.Equal(expected, HandGeometry.SegmentResizeAxis(hand, 1));
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

        /// <summary>The base is hit at the hand's own anchor.</summary>
        [Fact]
        public void HitTestFindsBase()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.Handle h = HandGeometry.HitTest(hand, new Vec2(101, 201), 6, 4, 24);

            Assert.Equal(HandGeometry.HandleKind.Base, h.Kind);
            Assert.Equal(0, h.Index);
        }

        /// <summary>A joint reports the index of the segment it terminates.</summary>
        [Fact]
        public void HitTestFindsJointBySegmentIndex()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true), (90, 40, false));

            HandGeometry.Handle first = HandGeometry.HitTest(hand, new Vec2(150, 200), 6, 4, 24);
            Assert.Equal(HandGeometry.HandleKind.Joint, first.Kind);
            Assert.Equal(1, first.Index);

            HandGeometry.Handle claw = HandGeometry.HitTest(hand, new Vec2(150, 240), 6, 4, 24);
            Assert.Equal(HandGeometry.HandleKind.Joint, claw.Kind);
            Assert.Equal(2, claw.Index);
        }

        /// <summary>A point along a bone but away from its joints reports the bone.</summary>
        [Fact]
        public void HitTestFindsBoneBetweenJoints()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.Handle h = HandGeometry.HitTest(hand, new Vec2(125, 201), 6, 4, 24);

            Assert.Equal(HandGeometry.HandleKind.Bone, h.Kind);
            Assert.Equal(1, h.Index);
        }

        /// <summary>Joints win over the bone they terminate.</summary>
        [Fact]
        public void HitTestPrefersJointOverBone()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            Assert.Equal(HandGeometry.HandleKind.Joint, HandGeometry.HitTest(hand, new Vec2(150, 200), 6, 4, 24).Kind);
        }

        /// <summary>The nub sits past the claw, continuing the last segment's direction.</summary>
        [Fact]
        public void NubContinuesLastSegmentDirection()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            Vec2 nub = HandGeometry.NubPosition(hand, 24);

            Assert.Equal(174, nub.X, 6);
            Assert.Equal(200, nub.Y, 6);
        }

        /// <summary>The nub is hit and reports the index of the segment it would create.</summary>
        [Fact]
        public void HitTestFindsNub()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.Handle h = HandGeometry.HitTest(hand, new Vec2(174, 200), 6, 4, 24);

            Assert.Equal(HandGeometry.HandleKind.Nub, h.Kind);
            Assert.Equal(2, h.Index);
        }

        /// <summary>A point away from every handle reports None.</summary>
        [Fact]
        public void HitTestReturnsNoneWhenClear()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            Assert.Equal(HandGeometry.HandleKind.None, HandGeometry.HitTest(hand, new Vec2(400, 400), 6, 4, 24).Kind);
        }

        /// <summary>Dragging the base rewrites only x/y, as whole numbers.</summary>
        [Fact]
        public void ApplyBaseDragWritesWholeCoordinates()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.ApplyBaseDrag(hand, new Vec2(161.6, 253.4));

            Assert.Equal(162, hand.X);
            Assert.Equal(253, hand.Y);
            Assert.Equal("0", hand.GetAttr("segment1Angle"));
        }

        /// <summary>An unmodified joint drag sets angle and length together, rounding the angle to whole degrees.</summary>
        [Fact]
        public void ApplyJointDragSetsAngleAndLengthFree()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.ApplyJointDrag(hand, 1, new Vec2(100, 130), snap: false);

            Assert.Equal("-90", hand.GetAttr("segment1Angle"));
            Assert.Equal("70", hand.GetAttr("segment1Length"));
        }

        /// <summary>A snapped joint drag rounds the angle to 90 degrees but leaves the length free.</summary>
        [Fact]
        public void ApplyJointDragSnapsAnglebutNotLength()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));
            HandGeometry.ApplyJointDrag(hand, 1, new Vec2(163, 205), snap: true);

            Assert.Equal("0", hand.GetAttr("segment1Angle"));
            Assert.Equal("63", hand.GetAttr("segment1Length"));
        }

        /// <summary>Dragging a joint leaves every other segment's angle untouched, because angles are absolute.</summary>
        [Fact]
        public void ApplyJointDragDoesNotChangeDownstreamAngles()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true), (90, 40, false));
            HandGeometry.ApplyJointDrag(hand, 1, new Vec2(100, 150), snap: true);

            Assert.Equal("90", hand.GetAttr("segment2Angle"));
            Assert.Equal("40", hand.GetAttr("segment2Length"));
        }

        /// <summary>Length dragging projects onto the existing segment ray without rewriting its angle.</summary>
        [Fact]
        public void ApplyLengthDragChangesOnlyLengthAlongCurrentAngle()
        {
            LevelObject hand = Hand(100, 200, (-90, 50, true), (0, 40, false));
            hand.SetAttr("segment1Angle", "-90.0");

            HandGeometry.ApplyLengthDrag(hand, 1, new Vec2(100, 120));

            Assert.Equal("-90.0", hand.GetAttr("segment1Angle"));
            Assert.Equal("80", hand.GetAttr("segment1Length"));
            Assert.Equal("0", hand.GetAttr("segment2Angle"));
            Assert.Equal("40", hand.GetAttr("segment2Length"));
        }

        /// <summary>Dragging behind the segment origin clamps length without flipping its direction.</summary>
        [Fact]
        public void ApplyLengthDragBehindOriginClampsWithoutChangingAngle()
        {
            LevelObject hand = Hand(100, 200, (0, 50, true));

            HandGeometry.ApplyLengthDrag(hand, 1, new Vec2(50, 200));

            Assert.Equal("0", hand.GetAttr("segment1Angle"));
            Assert.Equal("1", hand.GetAttr("segment1Length"));
        }

        /// <summary>Splitting a bone preserves every joint position, including the claw.</summary>
        [Fact]
        public void SplitBonePreservesChainShape()
        {
            LevelObject hand = Hand(100, 200, (0, 60, true), (90, 40, false));
            Vec2 clawBefore = HandGeometry.ClawPosition(hand);

            int newIndex = HandGeometry.SplitBone(hand, 1, new Vec2(125, 200));

            Assert.Equal(1, newIndex);
            Assert.Equal(3, HandObject.SegmentCount(hand));
            Assert.Equal("0", hand.GetAttr("segment1Angle"));
            Assert.Equal("25", hand.GetAttr("segment1Length"));
            Assert.Equal("0", hand.GetAttr("segment2Angle"));
            Assert.Equal("35", hand.GetAttr("segment2Length"));
            Assert.Equal("90", hand.GetAttr("segment3Angle"));

            Vec2 clawAfter = HandGeometry.ClawPosition(hand);
            Assert.Equal(clawBefore.X, clawAfter.X, 6);
            Assert.Equal(clawBefore.Y, clawAfter.Y, 6);
        }

        /// <summary>A split inherits the original segment's rotatable flag on both halves.</summary>
        [Fact]
        public void SplitBoneInheritsRotatable()
        {
            LevelObject hand = Hand(100, 200, (0, 60, false));
            _ = HandGeometry.SplitBone(hand, 1, new Vec2(130, 200));

            Assert.Equal("false", hand.GetAttr("segment1Rotatable"));
            Assert.Equal("false", hand.GetAttr("segment2Rotatable"));
        }

        /// <summary>A split at the very end still leaves both halves at least one unit long.</summary>
        [Fact]
        public void SplitBoneClampsDegenerateHalves()
        {
            LevelObject hand = Hand(100, 200, (0, 60, true));
            _ = HandGeometry.SplitBone(hand, 1, new Vec2(400, 200));

            Assert.Equal("59", hand.GetAttr("segment1Length"));
            Assert.Equal("1", hand.GetAttr("segment2Length"));
        }

        /// <summary>Splitting an out-of-range bone is a no-op.</summary>
        [Fact]
        public void SplitBoneIgnoresOutOfRangeIndex()
        {
            LevelObject hand = Hand(100, 200, (0, 60, true));
            Assert.Equal(-1, HandGeometry.SplitBone(hand, 5, new Vec2(130, 200)));
            Assert.Equal(1, HandObject.SegmentCount(hand));
        }

        /// <summary>Appending continues the last segment's angle and returns the new index to drag.</summary>
        [Fact]
        public void AppendSegmentContinuesLastAngle()
        {
            LevelObject hand = Hand(100, 200, (90, 50, true));
            int index = HandGeometry.AppendSegment(hand);

            Assert.Equal(2, index);
            Assert.Equal(2, HandObject.SegmentCount(hand));
            Assert.Equal("90", hand.GetAttr("segment2Angle"));
            Assert.Equal("10", hand.GetAttr("segment2Length"));
        }

        /// <summary>Appending to an empty hand creates the first segment.</summary>
        [Fact]
        public void AppendSegmentSeedsFirstSegment()
        {
            LevelObject hand = Hand(100, 200);
            int index = HandGeometry.AppendSegment(hand);

            Assert.Equal(1, index);
            Assert.Equal(1, HandObject.SegmentCount(hand));
            Assert.Equal("0", hand.GetAttr("segment1Angle"));
        }
    }
}

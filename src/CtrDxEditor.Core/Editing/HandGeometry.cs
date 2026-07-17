using System;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Pure canvas geometry for the mechanical hand: the joint chain, hit-testing and drags. UI-free, like
    /// <see cref="ConveyorGeometry"/>.
    ///
    /// A hand is a kinematic chain rooted at its (x,y). Each segment stores an <b>absolute</b> world angle
    /// (the game's <c>AddSegmentWithLengthAngleRotatable</c> subtracts ancestor rotations at load), so
    /// editing one segment's angle never changes another's — downstream segments keep their orientation and
    /// simply translate.
    ///
    /// The chain is a fold of <see cref="ObjectRotation.KnobPosition"/>: the game computes
    /// <c>joint_i = joint_{i-1} + VectRotate((length, 0), angle)</c>, and <c>VectRotate</c> is
    /// <c>(x·cos − y·sin, x·sin + y·cos)</c>, which reduces to <c>joint_{i-1} + (length·cos θ, length·sin θ)</c>
    /// — exactly <c>KnobPosition</c> with a zero display offset and a positive stored-angle sign. This mirrors
    /// <c>MechanicalHand.JointAtIndexPosition</c>.
    ///
    /// The hand is deliberately absent from <see cref="RotationTable"/>: a <see cref="RotationSpec"/> names a
    /// single angle attribute per object, whereas a hand carries one per segment. Specs are synthesized per
    /// segment by <see cref="SegmentSpec"/> instead.
    /// </summary>
    public static class HandGeometry
    {
        /// <summary>Padding added around the joint chain when computing selection bounds, in level units.</summary>
        private const double BoundsPadding = 17;

        /// <summary>
        /// The rotation convention for segment <paramref name="index"/>. The game renders each segment at its
        /// stored angle with no offset; the 90-degree snap step matches every authored hand, whose angles are
        /// exclusively cardinal.
        /// </summary>
        /// <param name="index">The 1-based segment index.</param>
        /// <returns>A spec naming that segment's angle attribute.</returns>
        public static RotationSpec SegmentSpec(int index)
        {
            return new RotationSpec(
                DisplayOffset: 0,
                AttributeName: HandObject.AngleAttr(index),
                SnapStep: 90);
        }

        /// <summary>The world position of joint <paramref name="index"/>, where 0 is the hand base.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The joint index; clamped to the live segment count.</param>
        /// <returns>The joint position in level units.</returns>
        public static Vec2 Joint(LevelObject hand, int index)
        {
            Vec2 p = new(hand.X, hand.Y);
            int n = Math.Min(index, HandObject.SegmentCount(hand));
            for (int i = 1; i <= n; i++)
            {
                p = Step(p, hand, i);
            }
            return p;
        }

        /// <summary>Every joint from the base to the claw.</summary>
        /// <param name="hand">The hand object.</param>
        /// <returns>Joint positions, length <c>SegmentCount + 1</c>; never empty.</returns>
        public static Vec2[] Joints(LevelObject hand)
        {
            int n = HandObject.SegmentCount(hand);
            Vec2[] points = new Vec2[n + 1];
            points[0] = new Vec2(hand.X, hand.Y);
            for (int i = 1; i <= n; i++)
            {
                points[i] = Step(points[i - 1], hand, i);
            }
            return points;
        }

        /// <summary>
        /// The claw's world position, which is the last joint. The game's <c>clawOffset</c> is deliberately not
        /// modeled: it positions the runtime candy-attach point, while the claw visual is anchored on the joint.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <returns>The claw position in level units.</returns>
        public static Vec2 ClawPosition(LevelObject hand)
        {
            return Joints(hand)[^1];
        }

        /// <summary>Axis-aligned bounds enclosing the joint chain, padded for the claw and base sprites.</summary>
        /// <param name="hand">The hand object.</param>
        /// <returns>The selection bounds in level units.</returns>
        public static LevelBounds Bounds(LevelObject hand)
        {
            Vec2[] points = Joints(hand);
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (Vec2 p in points)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }
            return new LevelBounds(
                minX - BoundsPadding,
                minY - BoundsPadding,
                maxX - minX + (BoundsPadding * 2),
                maxY - minY + (BoundsPadding * 2));
        }

        private static Vec2 Step(Vec2 from, LevelObject hand, int index)
        {
            return ObjectRotation.KnobPosition(
                from, HandObject.Angle(hand, index), SegmentSpec(index), HandObject.Length(hand, index));
        }
    }
}

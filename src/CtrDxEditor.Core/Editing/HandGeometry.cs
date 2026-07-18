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

        /// <summary>Screen-space pointer travel required before a hand press becomes a drag.</summary>
        public const double DragThreshold = 2;

        /// <summary>The screen-axis resize cursor that best matches a segment's current direction.</summary>
        public enum ResizeAxis
        {
            /// <summary>Use the west/east resize cursor.</summary>
            Horizontal,

            /// <summary>Use the north/south resize cursor.</summary>
            Vertical,
        }

        /// <summary>
        /// The rotation convention for segment <paramref name="index"/>. The game renders each segment at its
        /// stored angle with no offset; the editor snaps dial drags in 15-degree increments when requested.
        /// </summary>
        /// <param name="index">The 1-based segment index.</param>
        /// <returns>A spec naming that segment's angle attribute.</returns>
        public static RotationSpec SegmentSpec(int index)
        {
            return new RotationSpec(
                DisplayOffset: 0,
                AttributeName: HandObject.AngleAttr(index),
                SnapStep: 15);
        }

        /// <summary>Chooses the nearest horizontal or vertical cursor axis for a segment's stored angle.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        /// <returns>Vertical between the 45° diagonals; horizontal at the boundaries and otherwise.</returns>
        public static ResizeAxis SegmentResizeAxis(LevelObject hand, int index)
        {
            if (index < 1 || index > HandObject.SegmentCount(hand))
            {
                return ResizeAxis.Horizontal;
            }

            double degrees = Math.Abs(ObjectRotation.Normalize(HandObject.Angle(hand, index)));
            return degrees is > 45 and < 135 ? ResizeAxis.Vertical : ResizeAxis.Horizontal;
        }

        /// <summary>What part of a hand a point is over, so the canvas can route a drag.</summary>
        public enum HandleKind
        {
            /// <summary>Nothing interactive under the point.</summary>
            None,

            /// <summary>The hand base: dragging moves the whole hand.</summary>
            Base,

            /// <summary>A joint: dragging rewrites its segment's angle and length together.</summary>
            Joint,

            /// <summary>A bone: clicking selects its segment, and Alt-clicking splits it into two collinear segments.</summary>
            Bone,
        }

        /// <summary>A hit-tested hand handle.</summary>
        /// <param name="Kind">Which handle kind is under the point.</param>
        /// <param name="Index">
        /// The 1-based segment index for <see cref="HandleKind.Joint"/> (the segment the joint terminates) and
        /// <see cref="HandleKind.Bone"/>; 0 for <see cref="HandleKind.Base"/>.
        /// </param>
        public readonly record struct Handle(HandleKind Kind, int Index);

        /// <summary>Returns whether two-dimensional pointer travel has crossed the screen-space drag threshold.</summary>
        /// <param name="start">Pointer position when the hand gesture began, in level units.</param>
        /// <param name="current">Current pointer position, in level units.</param>
        /// <param name="zoom">Screen pixels per level unit.</param>
        public static bool HasDragged(Vec2 start, Vec2 current, double zoom)
        {
            double dx = (current.X - start.X) * zoom;
            double dy = (current.Y - start.Y) * zoom;
            return (dx * dx) + (dy * dy) >= DragThreshold * DragThreshold;
        }

        /// <summary>
        /// Classifies what a point is over. Joints are scanned tip-first and beat the bones they terminate,
        /// then bones — mirroring the polyline editor's point-before-segment order. Tolerances are in level
        /// units, so the caller converts screen pixels via the zoom.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="point">The position to classify, in level units.</param>
        /// <param name="jointTolerance">Hit radius around joints and the base.</param>
        /// <param name="boneTolerance">Hit distance from a bone's centre line.</param>
        /// <returns>The handle under the point, or <see cref="HandleKind.None"/>.</returns>
        public static Handle HitTest(
            LevelObject hand, Vec2 point, double jointTolerance, double boneTolerance)
        {
            Vec2[] points = Joints(hand);

            for (int i = points.Length - 1; i >= 1; i--)
            {
                if (Distance(points[i], point) <= jointTolerance)
                {
                    return new Handle(HandleKind.Joint, i);
                }
            }

            if (Distance(points[0], point) <= jointTolerance)
            {
                return new Handle(HandleKind.Base, 0);
            }

            for (int i = 1; i < points.Length; i++)
            {
                if (DistanceToSegment(points[i - 1], points[i], point) <= boneTolerance)
                {
                    return new Handle(HandleKind.Bone, i);
                }
            }

            return new Handle(HandleKind.None, 0);
        }

        /// <summary>Moves the whole hand by rewriting its anchor; the chain follows unchanged.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="point">The new base position in level units.</param>
        public static void ApplyBaseDrag(LevelObject hand, Vec2 point)
        {
            hand.X = (int)Math.Round(point.X, MidpointRounding.AwayFromZero);
            hand.Y = (int)Math.Round(point.Y, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Rewrites segment <paramref name="index"/>'s angle and length from a single joint drag. Because
        /// angles are absolute, downstream segments keep their orientation and translate along automatically.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment whose end joint is being dragged.</param>
        /// <param name="point">The drag position in level units.</param>
        /// <param name="snap">True to snap the angle to the spec's 90-degree step; false to round to whole degrees.</param>
        public static void ApplyJointDrag(LevelObject hand, int index, Vec2 point, bool snap)
        {
            if (index < 1 || index > HandObject.SegmentCount(hand))
            {
                return;
            }

            Vec2 center = Joint(hand, index - 1);
            double angle = ObjectRotation.AngleFromPoint(center, point, SegmentSpec(index), snap);
            HandObject.SetAngle(hand, index, angle);
            HandObject.SetLength(hand, index, Distance(center, point));
        }

        /// <summary>
        /// Changes one segment's length by projecting the pointer onto its existing direction. The angle is
        /// never rewritten, and dragging behind the segment origin clamps the length to one unit rather than
        /// flipping the segment.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment whose end joint is being dragged.</param>
        /// <param name="point">The drag position in level units.</param>
        public static void ApplyLengthDrag(LevelObject hand, int index, Vec2 point)
        {
            if (index < 1 || index > HandObject.SegmentCount(hand))
            {
                return;
            }

            Vec2 center = Joint(hand, index - 1);
            double radians = HandObject.Angle(hand, index) * Math.PI / 180;
            double projected = ((point.X - center.X) * Math.Cos(radians))
                + ((point.Y - center.Y) * Math.Sin(radians));
            HandObject.SetLength(hand, index, Math.Max(1, projected));
        }

        /// <summary>
        /// Where bone <paramref name="index"/> would split for a cut at <paramref name="point"/>: the pointer
        /// projected onto the bone, clamped so neither half is shorter than one unit. Shared by the split
        /// itself and its hover preview so the ghost joint sits exactly where <see cref="SplitBone"/> lands it.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment being split.</param>
        /// <param name="point">The cut position in level units.</param>
        /// <returns>The insertion position in level units, or the bone start when <paramref name="index"/> is out of range.</returns>
        public static Vec2 SplitPoint(LevelObject hand, int index, Vec2 point)
        {
            Vec2 start = Joint(hand, index - 1);
            if (index < 1 || index > HandObject.SegmentCount(hand))
            {
                return start;
            }

            double along = AlongBone(hand, index, start, point);
            double radians = HandObject.Angle(hand, index) * Math.PI / 180;
            return new Vec2(start.X + (along * Math.Cos(radians)), start.Y + (along * Math.Sin(radians)));
        }

        /// <summary>
        /// Splits bone <paramref name="index"/> at <paramref name="point"/> into two collinear segments. Both
        /// halves inherit the original angle and rotatable flag, so the rendered arm is unchanged; only the
        /// joint count grows. Each half is kept at least one unit long.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment to split.</param>
        /// <param name="point">The split position in level units; projected onto the bone.</param>
        /// <returns>The inserted joint's index, ready to drag, or -1 when <paramref name="index"/> is out of range.</returns>
        public static int SplitBone(LevelObject hand, int index, Vec2 point)
        {
            if (index < 1 || index > HandObject.SegmentCount(hand))
            {
                return -1;
            }

            Vec2 start = Joint(hand, index - 1);
            double total = HandObject.Length(hand, index);
            double angle = HandObject.Angle(hand, index);
            bool rotatable = HandObject.Rotatable(hand, index);

            double along = AlongBone(hand, index, start, point);
            HandObject.SetLength(hand, index, along);
            HandObject.InsertSegment(hand, index + 1, angle, total - along, rotatable);
            return index;
        }

        /// <summary>The pointer's distance along bone <paramref name="index"/> from <paramref name="start"/>, clamped to leave each half at least one unit.</summary>
        private static double AlongBone(LevelObject hand, int index, Vec2 start, Vec2 point)
        {
            double total = HandObject.Length(hand, index);
            double radians = HandObject.Angle(hand, index) * Math.PI / 180;
            double along = ((point.X - start.X) * Math.Cos(radians)) + ((point.Y - start.Y) * Math.Sin(radians));
            return Math.Clamp(along, 1, Math.Max(1, total - 1));
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

        /// <summary>The base segment's absolute world angle, orienting the selection box along the arm.</summary>
        /// <param name="hand">The hand object.</param>
        /// <returns>Segment 1's angle in degrees, or 0 when the hand has no live segments.</returns>
        public static double BaseAngle(LevelObject hand)
        {
            return HandObject.SegmentCount(hand) >= 1 ? HandObject.Angle(hand, 1) : 0;
        }

        /// <summary>
        /// Bounds enclosing the joint chain, padded for the claw and base sprites, expressed in the arm's
        /// local frame (segment 1 laid flat along +X from the base). The selection renderer rotates this box
        /// back by <see cref="BaseAngle"/> around the base, so a straight arm gets a tight box that follows
        /// the arm's rotation rather than a large axis-aligned rectangle. A hand whose base angle is 0 yields
        /// the plain axis-aligned box.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <returns>The selection bounds in the arm's local frame, in level units.</returns>
        public static LevelBounds Bounds(LevelObject hand)
        {
            Vec2 baseJoint = new(hand.X, hand.Y);
            double radians = -BaseAngle(hand) * Math.PI / 180;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);

            Vec2[] points = Joints(hand);
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (Vec2 p in points)
            {
                double dx = p.X - baseJoint.X;
                double dy = p.Y - baseJoint.Y;
                double localX = baseJoint.X + (dx * cos) - (dy * sin);
                double localY = baseJoint.Y + (dx * sin) + (dy * cos);
                minX = Math.Min(minX, localX);
                minY = Math.Min(minY, localY);
                maxX = Math.Max(maxX, localX);
                maxY = Math.Max(maxY, localY);
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

        private static double Distance(Vec2 a, Vec2 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static double DistanceToSegment(Vec2 a, Vec2 b, Vec2 p)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSquared = (dx * dx) + (dy * dy);
            if (lengthSquared < 0.0001)
            {
                return Distance(a, p);
            }

            double t = Math.Clamp((((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / lengthSquared, 0, 1);
            return Distance(new Vec2(a.X + (t * dx), a.Y + (t * dy)), p);
        }
    }
}

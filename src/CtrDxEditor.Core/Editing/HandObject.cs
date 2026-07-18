using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Attribute access for the mechanical hand (<c>hand</c>). Segments live in 1-based numbered slots
    /// (<c>segment1Angle</c>, <c>segment1Length</c>, <c>segment1Rotatable</c>, ...) and <c>segmentsCount</c>
    /// gates how many the game reads (<c>LoadHands.cs</c> loops <c>i = 1..segmentsCount</c>). Slots past the
    /// count are dead data the game ignores; the editor preserves them verbatim rather than removing them.
    /// Parsing mirrors the game exactly: angle/length fall back to 0, and rotatable is false for anything
    /// <see cref="bool.TryParse(string, out bool)"/> rejects.
    /// </summary>
    public static class HandObject
    {
        /// <summary>The level XML tag the game dispatches on.</summary>
        public const string ElementName = "hand";

        /// <summary>The XML attribute gating how many segment slots the game reads.</summary>
        public const string CountAttr = "segmentsCount";

        /// <summary>Angle seeded into an absent slot, matching authored inactive slots.</summary>
        public const string DefaultAngle = "0";

        /// <summary>Length seeded into an absent slot, long enough that a newly added segment is easy to see.</summary>
        public const string DefaultLength = "50";

        /// <summary>Rotatable flag seeded into an absent slot, matching authored inactive slots.</summary>
        public const string DefaultRotatable = "true";

        /// <summary>Whether <paramref name="type"/> is a mechanical hand element.</summary>
        /// <param name="type">The XML element name to test.</param>
        public static bool IsHand(string type)
        {
            return type == ElementName;
        }

        /// <summary>The attribute holding segment <paramref name="index"/>'s angle.</summary>
        /// <param name="index">The 1-based segment index.</param>
        public static string AngleAttr(int index)
        {
            return $"segment{index.ToString(CultureInfo.InvariantCulture)}Angle";
        }

        /// <summary>The attribute holding segment <paramref name="index"/>'s length.</summary>
        /// <param name="index">The 1-based segment index.</param>
        public static string LengthAttr(int index)
        {
            return $"segment{index.ToString(CultureInfo.InvariantCulture)}Length";
        }

        /// <summary>The attribute holding segment <paramref name="index"/>'s rotatable flag.</summary>
        /// <param name="index">The 1-based segment index.</param>
        public static string RotatableAttr(int index)
        {
            return $"segment{index.ToString(CultureInfo.InvariantCulture)}Rotatable";
        }

        /// <summary>The number of live segments; 0 when missing, unparseable, or negative.</summary>
        /// <param name="hand">The hand object.</param>
        public static int SegmentCount(LevelObject hand)
        {
            return int.TryParse(
                hand.GetAttr(CountAttr), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0
                ? n
                : 0;
        }

        /// <summary>
        /// Sets the live segment count, seeding any absent slot with the authored inactive defaults.
        /// Existing slot values are never overwritten, so shrinking then re-growing restores the old values.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="count">The new live segment count; negatives clamp to 0.</param>
        public static void SetSegmentCount(LevelObject hand, int count)
        {
            int n = Math.Max(0, count);
            for (int i = 1; i <= n; i++)
            {
                SeedSlot(hand, i);
            }
            hand.SetAttr(CountAttr, n.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Inserts a segment at <paramref name="index"/>, shifting the live slots at and above it up by one
        /// and incrementing the count. Only live slots shift, so a shift may overwrite a dead slot's leftover
        /// values; that is accepted loss for an explicit structural edit. Out-of-range indices are ignored.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based position the new segment takes; valid range is 1..count+1.</param>
        /// <param name="angle">The new segment's absolute world angle in degrees.</param>
        /// <param name="length">The new segment's length in level units.</param>
        /// <param name="rotatable">Whether the new segment is rotatable.</param>
        public static void InsertSegment(LevelObject hand, int index, double angle, double length, bool rotatable)
        {
            int n = SegmentCount(hand);
            if (index < 1 || index > n + 1)
            {
                return;
            }

            for (int i = n; i >= index; i--)
            {
                CopySlot(hand, i, i + 1);
            }

            SetAngle(hand, index, angle);
            SetLength(hand, index, length);
            SetRotatable(hand, index, rotatable);
            hand.SetAttr(CountAttr, (n + 1).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Deletes segment <paramref name="index"/>, shifting the live slots above it down by one and
        /// decrementing the count. Deleting the last segment shifts nothing, leaving its slot verbatim as a
        /// preserved orphan. Out-of-range indices are ignored.
        /// </summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment to remove; valid range is 1..count.</param>
        public static void DeleteSegment(LevelObject hand, int index)
        {
            int n = SegmentCount(hand);
            if (index < 1 || index > n)
            {
                return;
            }

            for (int i = index; i < n; i++)
            {
                CopySlot(hand, i + 1, i);
            }

            hand.SetAttr(CountAttr, (n - 1).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Segment <paramref name="index"/>'s absolute world angle in degrees; 0 when missing or unparseable.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        public static double Angle(LevelObject hand, int index)
        {
            return Float(hand, AngleAttr(index));
        }

        /// <summary>Segment <paramref name="index"/>'s length in level units; 0 when missing or unparseable.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        public static double Length(LevelObject hand, int index)
        {
            return Float(hand, LengthAttr(index));
        }

        /// <summary>Whether segment <paramref name="index"/> exposes an in-game rotate button.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        public static bool Rotatable(LevelObject hand, int index)
        {
            return bool.TryParse(hand.GetAttr(RotatableAttr(index)), out bool v) && v;
        }

        /// <summary>Writes segment <paramref name="index"/>'s angle as invariant whole degrees.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        /// <param name="degrees">The absolute world angle to store.</param>
        public static void SetAngle(LevelObject hand, int index, double degrees)
        {
            hand.SetAttr(AngleAttr(index), ObjectRotation.Format(degrees));
        }

        /// <summary>Writes segment <paramref name="index"/>'s length as an invariant whole number, floored at 1.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        /// <param name="length">The length in level units.</param>
        public static void SetLength(LevelObject hand, int index, double length)
        {
            long whole = (long)Math.Round(Math.Max(1, length), MidpointRounding.AwayFromZero);
            hand.SetAttr(LengthAttr(index), whole.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Writes segment <paramref name="index"/>'s rotatable flag as strict lowercase true/false.</summary>
        /// <param name="hand">The hand object.</param>
        /// <param name="index">The 1-based segment index.</param>
        /// <param name="value">Whether the segment is rotatable.</param>
        public static void SetRotatable(LevelObject hand, int index, bool value)
        {
            hand.SetAttr(RotatableAttr(index), value ? "true" : "false");
        }

        private static void SeedSlot(LevelObject hand, int index)
        {
            if (hand.GetAttr(AngleAttr(index)) is null)
            {
                hand.SetAttr(AngleAttr(index), DefaultAngle);
            }
            if (hand.GetAttr(LengthAttr(index)) is null)
            {
                hand.SetAttr(LengthAttr(index), DefaultLength);
            }
            if (hand.GetAttr(RotatableAttr(index)) is null)
            {
                hand.SetAttr(RotatableAttr(index), DefaultRotatable);
            }
        }

        private static void CopySlot(LevelObject hand, int from, int to)
        {
            SetAngle(hand, to, Angle(hand, from));
            SetLength(hand, to, Length(hand, from));
            SetRotatable(hand, to, Rotatable(hand, from));
        }

        private static double Float(LevelObject hand, string attr)
        {
            return double.TryParse(
                hand.GetAttr(attr), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v
                : 0;
        }
    }
}

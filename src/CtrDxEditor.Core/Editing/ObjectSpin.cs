using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for the game's <c>rotateSpeed</c> attribute, exposed as Spin in the editor.</summary>
    public static class ObjectSpin
    {
        /// <summary>Default spin speed written when enabling spin on an object without an existing speed.</summary>
        public const int DefaultSpeed = 70;

        /// <summary>Whether <paramref name="obj"/> has an active non-zero spin speed.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute is inspected.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> stores a non-zero speed.</returns>
        public static bool IsSpinning(LevelObject obj)
        {
            return RawSpeed(obj) != 0;
        }

        /// <summary>The positive whole-number spin speed magnitude shown in the editor.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute is inspected.</param>
        /// <returns>The absolute whole-number speed, or zero when absent or invalid.</returns>
        public static int Speed(LevelObject obj)
        {
            return Math.Abs(RawSpeed(obj));
        }

        /// <summary>Whether the stored speed is clockwise. Zero defaults to clockwise for new spins.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute is inspected.</param>
        /// <returns><see langword="true"/> when the stored speed is zero or positive.</returns>
        public static bool Clockwise(LevelObject obj)
        {
            return RawSpeed(obj) >= 0;
        }

        /// <summary>Writes or clears <c>rotateSpeed</c>, storing direction as the sign of <paramref name="speed"/>.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute is updated.</param>
        /// <param name="enabled">Whether spin should remain enabled.</param>
        /// <param name="speed">Positive whole-number spin speed magnitude.</param>
        /// <param name="clockwise">Whether the stored speed should be positive.</param>
        public static void SetSpin(LevelObject obj, bool enabled, int speed, bool clockwise)
        {
            if (!enabled || speed <= 0)
            {
                obj.RemoveAttr("rotateSpeed");
                return;
            }

            int signed = clockwise ? speed : -speed;
            obj.SetAttr("rotateSpeed", signed.ToString(CultureInfo.InvariantCulture));
        }

        private static int RawSpeed(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("rotateSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? (int)value
                : 0;
        }
    }
}

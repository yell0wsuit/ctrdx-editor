using System;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Resolves which axe a grab's rope binds to, porting the game's <c>AxeGrabBinding</c>.
    /// </summary>
    /// <remarks>
    /// An explicit <c>axeNumber</c> always wins. Levels imported from Time Travel instead carry
    /// <c>axed="true"</c> and reuse <c>candyNumber</c> as the axe key; the editor honours that on load
    /// so imported levels resolve correctly, but never writes it - the "Attach to" control only ever
    /// produces an explicit <c>axeNumber</c>.
    /// </remarks>
    public static class AxeBinding
    {
        /// <summary>The XML element the game dispatches an axe on.</summary>
        public const string Element = "axe";

        /// <summary>The attribute keying an axe, and naming one from a grab.</summary>
        public const string KeyAttribute = "axeNumber";

        /// <summary>The imported Time Travel flag that reuses <c>candyNumber</c> as the axe key.</summary>
        public const string LegacyFlagAttribute = "axed";

        /// <summary>The axe key this grab asks for, or null when it binds to something else.</summary>
        /// <param name="grab">The grab to inspect.</param>
        /// <returns>The requested axe key, or null.</returns>
        public static string? RequestedKey(LevelObject grab)
        {
            return grab.GetAttr(KeyAttribute)
                ?? (IsTrue(grab.GetAttr(LegacyFlagAttribute)) ? grab.GetAttr("candyNumber") : null);
        }

        /// <summary>Whether <paramref name="obj"/> is an axe.</summary>
        /// <param name="obj">The object to test.</param>
        /// <returns><see langword="true"/> for an axe element.</returns>
        public static bool IsAxe(LevelObject obj)
        {
            return obj.Type == Element;
        }

        /// <summary>
        /// An axe's binding key. <c>LoadAxe</c> reads the attribute as <c>?? string.Empty</c>, so an
        /// axe authored without one is keyed by the empty string rather than being unkeyed - and a
        /// grab asking for <c>axeNumber=""</c> does find it.
        /// </summary>
        /// <param name="axe">The axe to key.</param>
        /// <returns>The axe's key, never null.</returns>
        public static string KeyOf(LevelObject axe)
        {
            return axe.GetAttr(KeyAttribute) ?? string.Empty;
        }

        /// <summary>
        /// Compares two binding keys the way the game's <c>CandyMatch</c> does: both must be present,
        /// and they are trimmed and compared case-insensitively.
        /// </summary>
        /// <param name="a">First key.</param>
        /// <param name="b">Second key.</param>
        /// <returns><see langword="true"/> when the two keys name the same object.</returns>
        public static bool KeyEquals(string? a, string? b)
        {
            return a is not null && b is not null
                && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrue(string? value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }
}

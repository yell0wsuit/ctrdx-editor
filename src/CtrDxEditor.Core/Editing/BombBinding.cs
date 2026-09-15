using System;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Resolves which bomb a grab's rope binds to, porting the game's <c>BombGrabBinding</c> and the bomb
    /// branch of <c>LoadGrabs</c>.
    /// </summary>
    /// <remarks>
    /// Unlike the axe, a bomb key alone binds nothing: <c>LoadGrabs</c> only looks a bomb up when the grab
    /// also carries <c>bombed="true"</c>. The key is an explicit <c>bombNumber</c> when present, otherwise
    /// the imported Time Travel spelling that reuses <c>candyNumber</c>. The "Attach to" control writes
    /// both <c>bombed="true"</c> and an explicit <c>bombNumber</c>.
    /// </remarks>
    public static class BombBinding
    {
        /// <summary>The XML element the game dispatches a bomb on.</summary>
        public const string Element = "bomb";

        /// <summary>The attribute keying a bomb, and naming one from a grab.</summary>
        public const string KeyAttribute = "bombNumber";

        /// <summary>The grab flag without which <c>LoadGrabs</c> never binds a bomb.</summary>
        public const string FlagAttribute = "bombed";

        /// <summary>The bomb key this grab asks for, or null when it binds to something else.</summary>
        /// <param name="grab">The grab to inspect.</param>
        /// <returns>The requested bomb key, or null.</returns>
        public static string? RequestedKey(LevelObject grab)
        {
            return IsTrue(grab.GetAttr(FlagAttribute))
                ? grab.GetAttr(KeyAttribute) ?? grab.GetAttr("candyNumber")
                : null;
        }

        /// <summary>Whether <paramref name="obj"/> is a bomb.</summary>
        /// <param name="obj">The object to test.</param>
        /// <returns><see langword="true"/> for a bomb element.</returns>
        public static bool IsBomb(LevelObject obj)
        {
            return obj.Type == Element;
        }

        /// <summary>
        /// A bomb's binding key. <c>LoadBomb</c> reads the attribute as <c>?? string.Empty</c>, so a bomb
        /// authored without one is keyed by the empty string.
        /// </summary>
        /// <param name="bomb">The bomb to key.</param>
        /// <returns>The bomb's key, never null.</returns>
        public static string KeyOf(LevelObject bomb)
        {
            return bomb.GetAttr(KeyAttribute) ?? string.Empty;
        }

        /// <summary>The game's <c>IsTruthy</c>: <c>true</c> in any case, or <c>1</c>.</summary>
        private static bool IsTrue(string? value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }
}

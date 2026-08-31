using System;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Reads a grab's chain state off its <c>breakable</c> attribute, matching the game's
    /// <c>LoadGrab</c>: the attribute defaults to <see langword="true"/> (an ordinary,
    /// finger-cuttable rope), so only an explicit <c>breakable="false"</c> marks a chain.
    /// </summary>
    /// <remarks>
    /// A chain does two things in the game. Its bungee is marked <c>SetCutOnlyByAxe</c>, so it draws
    /// as linked segments and survives every cut but the Time Travel axe's; and its hook picks up
    /// <c>HookModifiers.ChainAnchor</c>, which swaps the hook art for the chain variant. The second
    /// happens even when the grab has no authored rope, which is why an auto-catch chain grab still
    /// looks like a chain anchor.
    /// </remarks>
    public static class ChainRope
    {
        /// <summary>The XML attribute holding the state.</summary>
        public const string Attribute = "breakable";

        /// <summary>Whether this grab is a chain rather than an ordinary rope.</summary>
        /// <param name="grab">The grab to inspect; any object type is accepted.</param>
        /// <returns><see langword="true"/> only for an explicit falsy <c>breakable</c>.</returns>
        public static bool IsChain(LevelObject grab)
        {
            return grab.Type == "grab" && grab.GetAttr(Attribute) is { } value && !IsTruthy(value);
        }

        /// <summary>
        /// Turns the chain on or off, writing <c>breakable="false"</c> for a chain and removing the
        /// attribute for an ordinary rope. The game's default is <see langword="true"/>, so omitting
        /// it keeps the saved XML as close to the authored original as possible.
        /// </summary>
        /// <param name="grab">The grab to change.</param>
        /// <param name="chain">Whether the grab should be a chain.</param>
        public static void Set(LevelObject grab, bool chain)
        {
            if (chain)
            {
                grab.SetAttr(Attribute, "false");
            }
            else
            {
                grab.RemoveAttr(Attribute);
            }
        }

        // Mirrors LoadGrabs.IsTruthy: the game accepts "1" alongside "true".
        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }
    }
}

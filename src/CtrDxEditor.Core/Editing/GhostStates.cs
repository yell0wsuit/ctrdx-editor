using System.Collections.Generic;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A state a ghost may morph into (idle is implicit and never listed here).</summary>
    public enum GhostMorph
    {
        /// <summary>Morph into a bubble.</summary>
        Bubble,

        /// <summary>Morph into a grab (radius-only).</summary>
        Grab,

        /// <summary>Morph into a small bouncer (angle-only).</summary>
        Bouncer,
    }

    /// <summary>
    /// Reads which morph states a ghost permits, in the game's tap-cycle order (bubble, grab,
    /// bouncer — matching the bit-shift order in Ghost.ResetToNextState). Pure; UI-free.
    /// </summary>
    public static class GhostStates
    {
        /// <summary>Gets the ghost's enabled morphs in cycle order.</summary>
        /// <param name="ghost">The ghost object whose boolean morph attributes are read.</param>
        /// <returns>The enabled morphs in bubble, grab, bouncer order.</returns>
        public static IReadOnlyList<GhostMorph> Enabled(LevelObject ghost)
        {
            List<GhostMorph> states = [];
            if (IsOn(ghost, "bubble"))
            {
                states.Add(GhostMorph.Bubble);
            }
            if (IsOn(ghost, "grab"))
            {
                states.Add(GhostMorph.Grab);
            }
            if (IsOn(ghost, "bouncer"))
            {
                states.Add(GhostMorph.Bouncer);
            }
            return states;
        }

        /// <summary>Determines whether the ghost permits no morphs and will sit idle in game.</summary>
        /// <param name="ghost">The ghost object whose boolean morph attributes are read.</param>
        /// <returns><see langword="true"/> when no morph attributes are enabled.</returns>
        public static bool IsIdleOnly(LevelObject ghost)
        {
            return Enabled(ghost).Count == 0;
        }

        private static bool IsOn(LevelObject ghost, string attr)
        {
            return bool.TryParse(ghost.GetAttr(attr), out bool b) && b;
        }
    }
}

using System.Globalization;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Holds the ephemeral morph shown for the selected ghost. This state only chooses canvas
    /// presentation and is cleared on selection changes; it is never persisted to level XML.
    /// </summary>
    public sealed class GhostPreviewState
    {
        /// <summary>The morph currently previewed, or null to show the plain ghost sprite.</summary>
        public GhostMorph? Active { get; private set; }

        /// <summary>Activates a morph only when the ghost permits it.</summary>
        /// <param name="ghost">The selected ghost.</param>
        /// <param name="morph">The morph to preview.</param>
        public void Set(LevelObject ghost, GhostMorph morph)
        {
            if (GhostStates.Enabled(ghost).Contains(morph))
            {
                Active = morph;
            }
        }

        /// <summary>Reverts to the plain ghost sprite.</summary>
        public void Clear()
        {
            Active = null;
        }

        /// <summary>Gets the sprite key for the active morph, or null for the ghost body and face.</summary>
        public string? MorphSpriteKey => Active switch
        {
            GhostMorph.Grab => "grab_auto",
            GhostMorph.Bubble => "bubble",
            GhostMorph.Bouncer => "bouncer1",
            _ => null,
        };

        /// <summary>
        /// Gets the element name whose hitbox the active morph collides with, or null when it has none.
        /// A grab is a rope hook with no hitbox; bubble and bouncer1 both have HitboxTable rows.
        /// </summary>
        public string? MorphHitboxElement => Active switch
        {
            GhostMorph.Bubble => "bubble",
            GhostMorph.Bouncer => "bouncer1",
            GhostMorph.Grab => null,
            _ => null,
        };

        /// <summary>Determines whether the grab preview has a positive authored radius.</summary>
        /// <param name="ghost">The selected ghost.</param>
        /// <returns><see langword="true"/> when the radius ring should be drawn.</returns>
        public bool ShowsRadiusRing(LevelObject ghost)
        {
            return Active == GhostMorph.Grab && Radius(ghost) > 0;
        }

        /// <summary>Determines whether the bouncer preview needs its rotation dial.</summary>
        /// <param name="ghost">The selected ghost.</param>
        /// <returns><see langword="true"/> when the rotation dial should be drawn.</returns>
        public bool ShowsRotationDial(LevelObject ghost)
        {
            _ = ghost;
            return Active == GhostMorph.Bouncer;
        }

        private static double Radius(LevelObject ghost)
        {
            return double.TryParse(
                ghost.GetAttr("radius"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double radius)
                ? radius
                : -1;
        }
    }
}

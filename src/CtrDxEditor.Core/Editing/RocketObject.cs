using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Rocket (game element <c>rocket</c>) placement defaults. The game scales an authored impulse into
    /// world coordinates only under the Time Travel and mobile models
    /// (<c>ActivePhysicsConstants.RocketImpulseScale</c>); desktop Experiments values are already
    /// world-tuned, so a Time Travel level is authored with a much smaller number.
    /// </summary>
    public static class RocketObject
    {
        /// <summary>The XML element name for a rocket.</summary>
        public const string Element = "rocket";

        /// <summary>Default thrust used by palette placement.</summary>
        public const string DefaultImpulse = "20";

        /// <summary>Default thrust used by palette placement in a Time Travel rocket physics level.</summary>
        public const string TimeTravelDefaultImpulse = "5";

        /// <summary>Default thrust multiplier used by palette placement.</summary>
        public const string DefaultImpulseFactor = "0.6";

        /// <summary>
        /// The placement impulse for <paramref name="document"/>, honouring its Time Travel rocket
        /// physics flag.
        /// </summary>
        /// <param name="document">The level being edited, or null when no level context is available.</param>
        /// <returns>The impulse to author on a freshly placed rocket.</returns>
        public static string ImpulseFor(LevelDocument? document)
        {
            return document?.UseTimeTravelRocketPhysics == true ? TimeTravelDefaultImpulse : DefaultImpulse;
        }

        /// <summary>The rockets in <paramref name="document"/>, in document order.</summary>
        /// <param name="document">The level to search.</param>
        /// <returns>Every rocket the level holds.</returns>
        public static IEnumerable<LevelObject> RocketsIn(LevelDocument document)
        {
            return document.AllObjects.Where(o => o.Type == Element);
        }

        /// <summary>
        /// Whether a settings change turned Time Travel rocket physics off on a level that already holds
        /// rockets. Their authored impulse was chosen against that tuning and is deliberately left
        /// untouched, so the editor points the author at it instead of rewriting it.
        /// </summary>
        /// <param name="before">The level's settings before the change.</param>
        /// <param name="after">The settings the author confirmed.</param>
        /// <param name="document">The level being edited.</param>
        /// <returns>True when the author should be reminded to revisit rocket impulses.</returns>
        public static bool ImpulseNeedsReview(LevelSettings before, LevelSettings after, LevelDocument document)
        {
            return before.UseTimeTravelRocketPhysics
                && !after.UseTimeTravelRocketPhysics
                && RocketsIn(document).Any();
        }
    }
}

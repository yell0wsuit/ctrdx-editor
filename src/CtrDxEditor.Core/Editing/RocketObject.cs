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
    }
}

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// The rope constants that differ between the game's two physics models, in level units.
    /// </summary>
    /// <remarks>
    /// A level picks its model with <c>useMobilePhysics</c>, and the game reads these through
    /// <c>ActivePhysicsConstants</c>, so a rope is subdivided and shaded differently under each. The
    /// editor draws in raw XML space while the game loads levels at mapScale 3, so every world-space
    /// constant is divided back down here.
    /// </remarks>
    /// <param name="RestLength">
    /// <c>BungeeRestLength</c>: how much rope one physics part spans. It sets the part count, and with
    /// it how many bezier control points a cord bends through and how many links a chain draws.
    /// </param>
    /// <param name="SamplesPerSegment">
    /// <c>BungeeDrawSamplePoints</c>: bezier samples per control-point segment along a cord. A chain
    /// ignores this - <c>Bungee.ChainDrawSamplePoints</c> is a plain const 2 under both models.
    /// </param>
    /// <param name="StretchThresholdRatio">
    /// How far past its rest length a rope stretches before it reddens, as a fraction of that rest
    /// length. The game compares one segment against <c>restLength + BungeeStretchRedThreshold</c>; a
    /// taut rope stretches uniformly, so the ratio applies to the whole cord just as well.
    /// </param>
    public readonly record struct RopePhysics(double RestLength, int SamplesPerSegment, double StretchThresholdRatio)
    {
        // The game's world-space values. Desktop reads PhysicsConstants directly; mobile scales its raw
        // WP7 tuning by Wp7ToWorldScale (3), which is why 30 becomes 90 and 7 becomes 21.
        private const double MapScale = 3.0;

        /// <summary>The desktop model, which is what a level uses unless it opts into mobile physics.</summary>
        public static RopePhysics Desktop { get; } = new(105 / MapScale, 4, 7.0 / 105.0);

        /// <summary>The mobile/WP7 model, selected by <c>useMobilePhysics</c>.</summary>
        public static RopePhysics Mobile { get; } = new(90 / MapScale, 3, 21.0 / 90.0);

        /// <summary>The model a level is using.</summary>
        /// <param name="useMobilePhysics">The level's <c>useMobilePhysics</c> setting.</param>
        /// <returns>The matching constants.</returns>
        public static RopePhysics For(bool useMobilePhysics)
        {
            return useMobilePhysics ? Mobile : Desktop;
        }
    }
}

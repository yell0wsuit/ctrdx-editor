namespace CtrDxEditor.Core.Document
{
    /// <summary>Level-wide physics defaults the game applies when a level leaves them unset.</summary>
    public static class LevelGravity
    {
        /// <summary>Horizontal gravity applied when <c>globalGravityX</c> is absent: none.</summary>
        public const float DefaultX = 0f;

        /// <summary>
        /// Vertical gravity applied when <c>globalGravityY</c> is absent, matching the game's
        /// <c>GravityEarthY</c> (9.8 m/s² scaled by the 80 px-per-metre constant). Positive pulls downward.
        /// </summary>
        public const float DefaultY = 784f;
    }

    /// <summary>The editable level-wide settings written into the settings layer.</summary>
    /// <param name="Width">Level width in map units.</param>
    /// <param name="Height">Level height in map units.</param>
    /// <param name="RopePhysicsSpeed">Rope simulation speed multiplier.</param>
    /// <param name="Special">Special tutorial-trigger identifier.</param>
    /// <param name="TwoParts">Whether the level uses the split two-candy layout.</param>
    /// <param name="NightLevel">Whether the level uses night-level visuals.</param>
    /// <param name="UseMobilePhysics">Whether the level requests the mobile physics model.</param>
    /// <param name="Water">
    /// Height of the bottom-pinned water band in level units; 0 means the level has no water.
    /// </param>
    /// <param name="WaterSpeed">
    /// Rate at which the water <em>drains</em>, in level units per second; 0 means a static pool.
    /// </param>
    /// <param name="LevelName">
    /// The level's display name, shown in-game and in rich presence; empty means the level has none.
    /// Shipped packs put a localization key here, but a hand-authored name passes through verbatim.
    /// </param>
    /// <param name="GravityX">
    /// Horizontal gravity applied to the level, positive pointing right;
    /// <see cref="LevelGravity.DefaultX"/> is the game's default.
    /// </param>
    /// <param name="GravityY">
    /// Vertical gravity applied to the level, positive pulling downward;
    /// <see cref="LevelGravity.DefaultY"/> is normal Earth gravity and 0 is weightless.
    /// </param>
    /// <param name="GridSize">Grid size used for the editor.</param>
    public sealed record LevelSettings(
        int Width,
        int Height,
        float RopePhysicsSpeed,
        int Special,
        bool TwoParts,
        bool NightLevel,
        bool UseMobilePhysics = false,
        float Water = 0f,
        float WaterSpeed = 0f,
        string LevelName = "",
        float GravityX = LevelGravity.DefaultX,
        float GravityY = LevelGravity.DefaultY,
        int GridSize = 32);
}

namespace CtrDxEditor.Core.Document
{
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
    public sealed record LevelSettings(
        int Width,
        int Height,
        float RopePhysicsSpeed,
        int Special,
        bool TwoParts,
        bool NightLevel,
        bool UseMobilePhysics = false,
        float Water = 0f,
        float WaterSpeed = 0f);
}

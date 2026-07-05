namespace CtrDxEditor.Core.Document
{
    /// <summary>The editable level-wide settings written into the settings layer.</summary>
    public sealed record LevelSettings(
        int Width,
        int Height,
        float RopePhysicsSpeed,
        int Special,
        bool TwoParts,
        bool NightLevel);
}

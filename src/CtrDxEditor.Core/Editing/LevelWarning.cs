namespace CtrDxEditor.Core.Editing
{
    /// <summary>How much a validator finding costs the level.</summary>
    public enum LevelWarningSeverity
    {
        /// <summary>The level plays, but something looks wrong.</summary>
        Warning,

        /// <summary>The game will drop this content: it is authored, but the player never sees it.</summary>
        Error,
    }

    /// <summary>
    /// A single structural warning from <see cref="LevelValidator"/>: a localization key plus any
    /// format arguments the message needs. Core stays presentation-agnostic - the UI layer resolves
    /// <see cref="Key"/> to text and substitutes <see cref="Args"/>.
    /// </summary>
    public sealed record LevelWarning(string Key, params object[] Args)
    {
        /// <summary>How much this finding costs the level. Warnings are advisory; errors lose content.</summary>
        public LevelWarningSeverity Severity { get; init; } = LevelWarningSeverity.Warning;
    }
}

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// A single structural warning from <see cref="LevelValidator"/>: a localization key plus any
    /// format arguments the message needs. Core stays presentation-agnostic - the UI layer resolves
    /// <see cref="Key"/> to text and substitutes <see cref="Args"/>.
    /// </summary>
    public sealed record LevelWarning(string Key, params object[] Args);
}

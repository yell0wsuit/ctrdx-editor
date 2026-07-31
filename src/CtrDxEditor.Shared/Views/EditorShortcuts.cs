using Avalonia.Input;

namespace CtrDxEditor.Views
{
    /// <summary>An editor action that a keyboard shortcut can trigger.</summary>
    public enum EditorShortcut
    {
        /// <summary>No shortcut matched.</summary>
        None,

        /// <summary>Create a new level.</summary>
        New,

        /// <summary>Open a level from disk.</summary>
        Open,

        /// <summary>Save the current level.</summary>
        Save,

        /// <summary>Save the current level under a new name.</summary>
        SaveAs,

        /// <summary>Save a screenshot of the level.</summary>
        Screenshot,

        /// <summary>Play the current level in Cut the Rope: DX.</summary>
        Playtest,

        /// <summary>Close the current level.</summary>
        Close,

        /// <summary>Undo the last edit.</summary>
        Undo,

        /// <summary>Redo the last undone edit.</summary>
        Redo,

        /// <summary>Select every object in the active layer.</summary>
        SelectAll,

        /// <summary>Copy the current object selection.</summary>
        Copy,

        /// <summary>Cut the current object selection.</summary>
        Cut,

        /// <summary>Paste copied objects at the canvas target.</summary>
        Paste,

        /// <summary>Zoom the canvas in.</summary>
        ZoomIn,

        /// <summary>Zoom the canvas out.</summary>
        ZoomOut,

        /// <summary>Fit the level to the viewport.</summary>
        ZoomFit,

        /// <summary>Delete the selected object.</summary>
        Delete,

        /// <summary>Toggle live animation preview.</summary>
        ToggleAnimationPreview,
    }

    /// <summary>
    /// Pure keyboard-shortcut resolution, split by routing phase so the two are mutually exclusive:
    /// <see cref="ResolveCommand"/> maps the command-modifier menu chords (handled globally at the TopLevel),
    /// and <see cref="ResolveLocal"/> maps the no-modifier, focus-gated editor keys (handled on the bubble
    /// path). Neither touches view state, so both are unit-testable without a UI.
    /// </summary>
    public static class EditorShortcuts
    {
        /// <summary>
        /// Resolves a command-modifier menu chord. Returns <see cref="EditorShortcut.None"/> for any key that
        /// does not carry <paramref name="commandModifier"/> (⌘ on macOS, Ctrl elsewhere), so Delete/Space and
        /// bare typing fall through. On non-macOS, Ctrl+Y is an additional redo binding.
        /// </summary>
        /// <param name="key">The key that was pressed.</param>
        /// <param name="modifiers">The modifiers held with it.</param>
        /// <param name="commandModifier">The platform's command modifier: Meta on macOS, Control elsewhere.</param>
        /// <param name="isMacOS">Whether the host is macOS, which drops the Ctrl+Y redo binding.</param>
        /// <param name="dialogOpen">
        /// Whether a modal dialog is up. These chords are handled at the TopLevel in the tunnel phase, so
        /// without this guard they fire straight past the dialog: Ctrl+W would close the level behind it,
        /// and Ctrl+C/V/A would be swallowed before the dialog's own text fields ever saw them.
        /// </param>
        public static EditorShortcut ResolveCommand(Key key, KeyModifiers modifiers, KeyModifiers commandModifier, bool isMacOS, bool dialogOpen)
        {
            if (dialogOpen || !modifiers.HasFlag(commandModifier))
            {
                return EditorShortcut.None;
            }

            bool shift = modifiers.HasFlag(KeyModifiers.Shift);
#pragma warning disable IDE0072
            return key switch
            {
                Key.N when !shift => EditorShortcut.New,
                Key.O when !shift => EditorShortcut.Open,
                Key.S when !shift => EditorShortcut.Save,
                Key.S when shift => EditorShortcut.SaveAs,
                Key.P when shift => EditorShortcut.Screenshot,
                Key.P when !shift => EditorShortcut.Playtest,
                Key.W when !shift => EditorShortcut.Close,
                Key.Z when !shift => EditorShortcut.Undo,
                Key.Z when shift => EditorShortcut.Redo,
                Key.Y when !isMacOS => EditorShortcut.Redo,
                Key.A when !shift => EditorShortcut.SelectAll,
                Key.C when !shift => EditorShortcut.Copy,
                Key.X when !shift => EditorShortcut.Cut,
                Key.V when !shift => EditorShortcut.Paste,
                Key.OemPlus or Key.Add => EditorShortcut.ZoomIn,
                Key.OemMinus or Key.Subtract => EditorShortcut.ZoomOut,
                Key.D0 or Key.NumPad0 => EditorShortcut.ZoomFit,
                _ => EditorShortcut.None,
            };
#pragma warning restore IDE0072
        }

        /// <summary>
        /// Resolves a no-modifier editor key. Delete maps to <see cref="EditorShortcut.Delete"/>; Space toggles
        /// animation preview only when no text input is focused (so it still types spaces and activates focused
        /// controls). Returns <see cref="EditorShortcut.None"/> for command-modifier chords.
        /// </summary>
        public static EditorShortcut ResolveLocal(Key key, KeyModifiers modifiers, bool textInputFocused)
        {
#pragma warning disable IDE0072
            return key switch
            {
                Key.Delete => EditorShortcut.Delete,
                Key.Space when modifiers == KeyModifiers.None && !textInputFocused => EditorShortcut.ToggleAnimationPreview,
                _ => EditorShortcut.None,
            };
#pragma warning restore IDE0072
        }
    }
}

using Avalonia.Input;

using CtrDxEditor.Views;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the pure keyboard-shortcut resolver that maps key chords to editor actions.</summary>
    public class EditorShortcutsTests
    {
        /// <summary>Each command-modifier chord resolves to its editor action.</summary>
        [Theory]
        [InlineData(Key.N, KeyModifiers.Control, EditorShortcut.New)]
        [InlineData(Key.O, KeyModifiers.Control, EditorShortcut.Open)]
        [InlineData(Key.S, KeyModifiers.Control, EditorShortcut.Save)]
        [InlineData(Key.S, KeyModifiers.Control | KeyModifiers.Shift, EditorShortcut.SaveAs)]
        [InlineData(Key.P, KeyModifiers.Control | KeyModifiers.Shift, EditorShortcut.Screenshot)]
        [InlineData(Key.W, KeyModifiers.Control, EditorShortcut.Close)]
        [InlineData(Key.Z, KeyModifiers.Control, EditorShortcut.Undo)]
        [InlineData(Key.Z, KeyModifiers.Control | KeyModifiers.Shift, EditorShortcut.Redo)]
        [InlineData(Key.A, KeyModifiers.Control, EditorShortcut.SelectAll)]
        [InlineData(Key.OemPlus, KeyModifiers.Control, EditorShortcut.ZoomIn)]
        [InlineData(Key.Add, KeyModifiers.Control, EditorShortcut.ZoomIn)]
        [InlineData(Key.OemMinus, KeyModifiers.Control, EditorShortcut.ZoomOut)]
        [InlineData(Key.Subtract, KeyModifiers.Control, EditorShortcut.ZoomOut)]
        [InlineData(Key.D0, KeyModifiers.Control, EditorShortcut.ZoomFit)]
        [InlineData(Key.NumPad0, KeyModifiers.Control, EditorShortcut.ZoomFit)]
        public void ResolveCommandMapsChords(Key key, KeyModifiers modifiers, EditorShortcut expected)
        {
            Assert.Equal(expected, EditorShortcuts.ResolveCommand(key, modifiers, KeyModifiers.Control, isMacOS: false));
        }

        /// <summary>Keys without the command modifier (bare typing, Shift-only) are never command chords.</summary>
        [Fact]
        public void ResolveCommandRequiresCommandModifier()
        {
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveCommand(Key.S, KeyModifiers.None, KeyModifiers.Control, false));
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveCommand(Key.N, KeyModifiers.Shift, KeyModifiers.Control, false));
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveCommand(Key.Delete, KeyModifiers.None, KeyModifiers.Control, false));
        }

        /// <summary>The command modifier is platform-supplied: on macOS it is Meta, so Ctrl there is not a command.</summary>
        [Fact]
        public void ResolveCommandHonorsPlatformModifier()
        {
            Assert.Equal(EditorShortcut.Save, EditorShortcuts.ResolveCommand(Key.S, KeyModifiers.Meta, KeyModifiers.Meta, isMacOS: true));
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveCommand(Key.S, KeyModifiers.Control, KeyModifiers.Meta, isMacOS: true));
        }

        /// <summary>Ctrl+Y is an extra redo binding on Windows/Linux only; macOS uses Cmd+Shift+Z.</summary>
        [Fact]
        public void CtrlYRedoIsNonMacOnly()
        {
            Assert.Equal(EditorShortcut.Redo, EditorShortcuts.ResolveCommand(Key.Y, KeyModifiers.Control, KeyModifiers.Control, isMacOS: false));
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveCommand(Key.Y, KeyModifiers.Meta, KeyModifiers.Meta, isMacOS: true));
        }

        /// <summary>Delete and unmodified Space resolve to their editor actions.</summary>
        [Fact]
        public void ResolveLocalMapsDeleteAndSpace()
        {
            Assert.Equal(EditorShortcut.Delete, EditorShortcuts.ResolveLocal(Key.Delete, KeyModifiers.None, textInputFocused: false));
            Assert.Equal(
                EditorShortcut.ToggleAnimationPreview,
                EditorShortcuts.ResolveLocal(Key.Space, KeyModifiers.None, textInputFocused: false));
        }

        /// <summary>Space toggles preview only when no text input is focused, so it still types spaces in fields.</summary>
        [Fact]
        public void SpaceYieldsToTextInput()
        {
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveLocal(Key.Space, KeyModifiers.None, textInputFocused: true));
        }

        /// <summary>A modified Space (e.g. Ctrl+Space) is not the preview toggle.</summary>
        [Fact]
        public void ModifiedSpaceIsNotAShortcut()
        {
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveLocal(Key.Space, KeyModifiers.Control, textInputFocused: false));
        }

        /// <summary>The local resolver ignores command chords so it can't double-fire with the global handler.</summary>
        [Fact]
        public void ResolveLocalIgnoresCommandChords()
        {
            Assert.Equal(EditorShortcut.None, EditorShortcuts.ResolveLocal(Key.S, KeyModifiers.Control, textInputFocused: false));
        }
    }
}

using System;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Platform-appropriate shortcut hint text for menu items: ⌘ on macOS, Ctrl elsewhere.
    /// Bound from XAML via <c>{x:Static}</c> so it needs no name-scope lookup.
    /// </summary>
    public static class ShortcutHint
    {
        private static readonly string Mod = OperatingSystem.IsMacOS() ? "⌘" : "Ctrl";

        /// <summary>Localized platform shortcut text for creating a new level.</summary>
        public static string New { get; } = $"{Mod} N";

        /// <summary>Localized platform shortcut text for opening a level.</summary>
        public static string Open { get; } = $"{Mod} O";

        /// <summary>Localized platform shortcut text for saving the level.</summary>
        public static string Save { get; } = $"{Mod} S";

        /// <summary>Localized platform shortcut text for saving the level under a new name.</summary>
        public static string SaveAs { get; } = $"{Mod} Shift S";

        /// <summary>Localized platform shortcut text for taking a screenshot.</summary>
        public static string Screenshot { get; } = $"{Mod} Shift P";

        /// <summary>Localized platform shortcut text for closing the level.</summary>
        public static string Close { get; } = $"{Mod} W";

        /// <summary>Localized platform shortcut text for zooming in.</summary>
        public static string ZoomIn { get; } = $"{Mod} +";

        /// <summary>Localized platform shortcut text for zooming out.</summary>
        public static string ZoomOut { get; } = $"{Mod} -";

        /// <summary>Localized platform shortcut text for fitting the level to the viewport.</summary>
        public static string ZoomFit { get; } = $"{Mod} 0";

        /// <summary>Localized platform shortcut text for undo.</summary>
        public static string Undo { get; } = $"{Mod} Z";

        /// <summary>Localized platform shortcut text for redo.</summary>
        public static string Redo { get; } = OperatingSystem.IsMacOS() ? $"{Mod} Shift Z" : $"{Mod} Y";

        /// <summary>Localized platform shortcut text for deleting the selected object.</summary>
        public static string Delete { get; } = "Delete";

        /// <summary>Localized platform shortcut text for toggling live animation preview.</summary>
        public static string AnimationPreview { get; } = "Space";
    }
}

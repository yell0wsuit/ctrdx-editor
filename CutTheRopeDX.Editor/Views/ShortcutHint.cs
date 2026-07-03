using System;

namespace CutTheRopeDX.Editor.Views
{
    /// <summary>
    /// Platform-appropriate shortcut hint text for menu items: ⌘ on macOS, Ctrl elsewhere.
    /// Bound from XAML via <c>{x:Static}</c> so it needs no name-scope lookup.
    /// </summary>
    public static class ShortcutHint
    {
        private static readonly string Mod = OperatingSystem.IsMacOS() ? "⌘" : "Ctrl";

        public static string ZoomIn { get; } = $"{Mod} +";
        public static string ZoomOut { get; } = $"{Mod} -";
        public static string ZoomFit { get; } = $"{Mod} 0";
    }
}

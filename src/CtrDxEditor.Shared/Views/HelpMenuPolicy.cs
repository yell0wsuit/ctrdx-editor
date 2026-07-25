using System;

namespace CtrDxEditor.Views
{
    /// <summary>Platform-specific visibility policy for commands in the Help surfaces.</summary>
    public static class HelpMenuPolicy
    {
        /// <summary>
        /// Whether Help should show About on the current platform. macOS owns About in its native
        /// application menu.
        /// </summary>
        public static bool ShowAboutInHelp { get; } = ShouldShowAboutInHelp(OperatingSystem.IsMacOS());

        /// <summary>Determines whether Help should contain an About command.</summary>
        /// <param name="isMacOS">Whether the host follows the macOS application-menu convention.</param>
        /// <returns><see langword="false"/> on macOS; otherwise <see langword="true"/>.</returns>
        public static bool ShouldShowAboutInHelp(bool isMacOS)
        {
            return !isMacOS;
        }
    }
}

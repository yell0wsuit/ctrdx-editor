using System;
using System.IO;

using CtrDxEditor.Localization;
using CtrDxEditor.Views;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests Help entry points and the platform-specific About policy.</summary>
    public class HelpMenuTests
    {
        /// <summary>macOS leaves About in the native application menu.</summary>
        [Fact]
        public void MacOsHidesAboutFromHelp()
        {
            Assert.False(HelpMenuPolicy.ShouldShowAboutInHelp(isMacOS: true));
        }

        /// <summary>Platforms without the native app-menu item expose About from Help.</summary>
        [Fact]
        public void OtherPlatformsShowAboutInHelp()
        {
            Assert.True(HelpMenuPolicy.ShouldShowAboutInHelp(isMacOS: false));
        }

        /// <summary>Help labels resolve through the shared localization catalog.</summary>
        /// <param name="key">Help-related localization key expected to resolve.</param>
        [Theory]
        [InlineData("Menu.Help")]
        [InlineData("Menu.Help.UsageGuide")]
        [InlineData("CommandDrawer.Help")]
        [InlineData("Dialog.UsageGuide.Title")]
        public void HelpLabelsAreLocalized(string key)
        {
            Assert.NotEqual(key, Localizer.Get(key));
        }

        /// <summary>Expanded and compact entry points route to shared Help handlers.</summary>
        [Fact]
        public void ExpandedAndCompactSurfacesWireHelpCommands()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string drawer = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.CommandDrawer.cs"));

            Assert.Contains("Click=\"UsageGuide_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"About_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"DrawerUsageGuide_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"DrawerAbout_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("UsageGuide_Click(sender, e)", drawer, StringComparison.Ordinal);
            Assert.Contains("About_Click(sender, e)", drawer, StringComparison.Ordinal);
        }

        /// <summary>The desktop guide stays modeless so readers can continue editing beside it.</summary>
        [Fact]
        public void DesktopUsageGuideIsModeless()
        {
            string commands = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "MainView.HelpCommands.cs"));

            Assert.Contains("new UsageGuideWindow().Show(owner);", commands, StringComparison.Ordinal);
            Assert.DoesNotContain("new UsageGuideWindow().ShowDialog(owner)", commands, StringComparison.Ordinal);
        }

        /// <summary>Locates a source file relative to the repository's <c>src</c> directory.</summary>
        /// <param name="parts">Path components beneath <c>src</c>.</param>
        /// <returns>Absolute path to the requested source file.</returns>
        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}

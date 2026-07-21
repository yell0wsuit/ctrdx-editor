using System;
using System.IO;
using System.Text.RegularExpressions;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests that dialogs can shrink to fit a phone. A fixed <c>Width</c> is a hard constraint in
    /// Avalonia: the control measures at that width regardless of available space and is clipped on a
    /// narrower screen, which is invisible on desktop and only shows up on a device.
    /// </summary>
    public class ResponsiveDialogTests
    {
        /// <summary>Every dialog whose markup is checked for shrinkability.</summary>
        public static TheoryData<string> DialogFiles =>
        [
            "ConfirmDialog.axaml",
            "MessageDialog.axaml",
            "PlaytestLocateDialog.axaml",
            "ContentSetupDialog.axaml",
            "LevelSettingsDialog.axaml",
            "ReviewChangesDialog.axaml",
        ];

        /// <summary>No dialog fixes the width of its root layout container.</summary>
        [Theory]
        [MemberData(nameof(DialogFiles))]
        public void DialogRootDoesNotFixItsWidth(string file)
        {
            string markup = ReadDialog(file);
            string root = RootElement(markup);

            Assert.DoesNotMatch(new Regex(@"\sWidth=""\d"), root);
        }

        /// <summary>Each dialog still caps its width, so desktop layout is unchanged.</summary>
        [Theory]
        [MemberData(nameof(DialogFiles))]
        public void DialogRootCapsItsWidth(string file)
        {
            string root = RootElement(ReadDialog(file));

            Assert.Matches(new Regex(@"MaxWidth=""\d+"""), root);
            Assert.Contains("HorizontalAlignment=\"Stretch\"", root, StringComparison.Ordinal);
        }

        // The root layout element is the first element after the UserControl open tag and any
        // Styles/Resources blocks - i.e. the first line that opens a panel with a Margin.
        private static string RootElement(string markup)
        {
            Match m = Regex.Match(markup, @"<(StackPanel|Grid|ScrollViewer)[^>]*Margin=""24""[^>]*>");
            Assert.True(m.Success, "Could not find the dialog's root layout element.");
            return m.Value;
        }

        private static string ReadDialog(string file)
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", file));
        }

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

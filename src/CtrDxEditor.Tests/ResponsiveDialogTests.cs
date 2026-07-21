using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using CtrDxEditor.Converters;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests that dialogs can shrink to fit a phone.
    /// </summary>
    /// <remarks>
    /// A literal <c>Width</c> is a hard constraint: the dialog measures at that width regardless of
    /// available space and is clipped on a narrower screen. A bare <c>MaxWidth</c> is the opposite
    /// failure — it only caps, so the dialog shrink-wraps its content and desktop layout changes. Only a
    /// width bound through <see cref="DialogSizeConverter"/> gives <c>min(preferred, available)</c>.
    /// </remarks>
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

        /// <summary>No dialog pins its root to a literal width.</summary>
        [Theory]
        [MemberData(nameof(DialogFiles))]
        public void DialogRootDoesNotFixItsWidth(string file)
        {
            string root = RootElement(ReadDialog(file));

            Assert.DoesNotMatch(new Regex(@"\sWidth=""\d"), root);
        }

        /// <summary>
        /// Each dialog derives its width from the top level through the clamping converter, so it keeps
        /// its desktop width while still fitting a narrow screen.
        /// </summary>
        [Theory]
        [MemberData(nameof(DialogFiles))]
        public void DialogRootClampsItsWidthToTheTopLevel(string file)
        {
            string root = RootElement(ReadDialog(file));

            Assert.Contains("$parent[TopLevel].Bounds.Width", root, StringComparison.Ordinal);
            Assert.Matches(new Regex(@"DialogSizeConverter\.Clamp\}, ConverterParameter=\d+"), root);
        }

        /// <summary>
        /// A bare MaxWidth on the root is the regression that shrink-wrapped every dialog: it caps a
        /// width but never establishes one, because alignment does not contribute to DesiredSize.
        /// </summary>
        [Theory]
        [MemberData(nameof(DialogFiles))]
        public void DialogRootDoesNotRelyOnMaxWidthAlone(string file)
        {
            string root = RootElement(ReadDialog(file));

            Assert.DoesNotMatch(new Regex(@"MaxWidth=""\d"), root);
        }

        /// <summary>The dialogs that can outgrow a short screen cap their height against the top level.</summary>
        [Theory]
        [InlineData("ContentSetupDialog.axaml")]
        [InlineData("LevelSettingsDialog.axaml")]
        [InlineData("ReviewChangesDialog.axaml")]
        public void TallDialogsClampTheirHeightToTheTopLevel(string file)
        {
            string markup = ReadDialog(file);

            Assert.Contains("$parent[TopLevel].Bounds.Height", markup, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"MaxHeight=""\d"), markup);
        }

        /// <summary>A window wider than the dialog yields the dialog's unchanged desktop width.</summary>
        [Fact]
        public void ClampKeepsThePreferredWidthOnADesktopWindow()
        {
            Assert.Equal(900d, Clamp(1920d, "900"));
        }

        /// <summary>A phone narrower than the dialog yields the window width less the margins.</summary>
        [Fact]
        public void ClampShrinksToTheWindowOnAPhone()
        {
            Assert.Equal(414d - DialogSizeConverter.Inset, Clamp(414d, "900"));
        }

        /// <summary>A landscape phone shorter than the dialog clamps height the same way.</summary>
        [Fact]
        public void ClampShrinksToTheWindowOnAShortScreen()
        {
            Assert.Equal(346d - DialogSizeConverter.Inset, Clamp(346d, "640"));
        }

        /// <summary>Exactly enough room for the dialog plus its margins is not treated as too little.</summary>
        [Fact]
        public void ClampAllowsTheExactFit()
        {
            Assert.Equal(380d, Clamp(380d + DialogSizeConverter.Inset, "380"));
        }

        /// <summary>An unmeasured or unusably small top level falls back to auto rather than a bad size.</summary>
        [Theory]
        [InlineData(double.NaN)]
        [InlineData(0d)]
        [InlineData(DialogSizeConverter.Inset)]
        public void ClampFallsBackToAutoWhenThereIsNoUsableSpace(double available)
        {
            Assert.Equal(double.NaN, Clamp(available, "380"));
        }

        /// <summary>A missing or malformed converter parameter falls back to auto.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("not-a-number")]
        [InlineData("0")]
        public void ClampFallsBackToAutoOnABadParameter(object? parameter)
        {
            Assert.Equal(double.NaN, Clamp(1920d, parameter));
        }

        private static double Clamp(object? available, object? parameter)
        {
            object result = DialogSizeConverter.Clamp.Convert(
                available, typeof(double), parameter, CultureInfo.InvariantCulture);
            return Assert.IsType<double>(result);
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

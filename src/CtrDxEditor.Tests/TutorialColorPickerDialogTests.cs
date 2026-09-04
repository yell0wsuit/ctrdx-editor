using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards the tutorial color dialog and its property-panel entry point.</summary>
    public class TutorialColorPickerDialogTests
    {
        /// <summary>The swatch is a discoverable button while the hex field remains directly editable.</summary>
        [Fact]
        public void ColorRowOpensPickerFromSwatchAndKeepsHexInput()
        {
            string markup = ReadSource("CtrDxEditor.Shared", "Views", "PropertyPanel.axaml");
            string code = ReadSource("CtrDxEditor.Shared", "Views", "PropertyPanel.axaml.cs");

            Assert.Contains("Click=\"ColorSwatch_Click\"", markup, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding Value, Mode=TwoWay}\"", markup, StringComparison.Ordinal);
            Assert.Contains("TutorialColorPickerDialog dialog = new", code, StringComparison.Ordinal);
        }

        /// <summary>The dialog exposes the native picker plus explicit apply, cancel and default actions.</summary>
        [Fact]
        public void DialogHasAllThreeRequestedActions()
        {
            string markup = ReadSource("CtrDxEditor.Shared", "Views", "TutorialColorPickerDialog.axaml");

            Assert.Contains("<ColorView", markup, StringComparison.Ordinal);
            Assert.Contains("Click=\"UseDefault_Click\"", markup, StringComparison.Ordinal);
            Assert.Contains("Click=\"Cancel_Click\"", markup, StringComparison.Ordinal);
            Assert.Contains("Click=\"Apply_Click\"", markup, StringComparison.Ordinal);
        }

        private static string ReadSource(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return File.ReadAllText(Path.Combine([path, .. parts]));
        }
    }
}

using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the reusable touch-friendly help affordance.</summary>
    public class PropertyHelpDialogTests
    {
        /// <summary>The shared control owns the standard icon, cursor, and modal interaction.</summary>
        [Fact]
        public void SharedHelpButtonOpensAMessageDialog()
        {
            string markup = ReadSource("CtrDxEditor.Shared", "Controls", "HelpButton.axaml");
            string codeBehind = ReadSource("CtrDxEditor.Shared", "Controls", "HelpButton.axaml.cs");

            Assert.Contains("Cursor=\"Hand\"", markup, StringComparison.Ordinal);
            Assert.Contains("Kind=\"HelpCircleOutline\" Width=\"16\" Height=\"16\"", markup,
                StringComparison.Ordinal);
            Assert.DoesNotContain("ToolTip.Tip", markup, StringComparison.Ordinal);
            Assert.Contains("Header = Header", codeBehind, StringComparison.Ordinal);
            Assert.Contains("Message = Message", codeBehind, StringComparison.Ordinal);
            Assert.Contains("_ = dialog.ShowAsync();", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Property fields reuse the shared control instead of maintaining their own handler.</summary>
        [Fact]
        public void PropertyFieldsUseSharedHelpButton()
        {
            string markup = ReadSource("CtrDxEditor.Shared", "Views", "PropertyPanel.axaml");
            string codeBehind = ReadSource("CtrDxEditor.Shared", "Views", "PropertyPanel.axaml.cs");

            Assert.Contains("<ctl:HelpButton", markup, StringComparison.Ordinal);
            Assert.Contains("Header=\"{Binding Label}\"", markup, StringComparison.Ordinal);
            Assert.Contains("Message=\"{Binding HelpText}\"", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("PropertyHelp_Click", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("PropertyHelp_Click", codeBehind, StringComparison.Ordinal);
        }

        private static string ReadSource(params string[] parts)
        {
            string path = SourcePath(parts);
            Assert.True(File.Exists(path), $"Expected shared help source: {path}");
            return File.ReadAllText(path);
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

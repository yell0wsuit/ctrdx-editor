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
        }

        /// <summary>Help uses DialogHost's official multi-dialog stack instead of replacing its parent session.</summary>
        [Fact]
        public void SharedHelpButtonUsesOfficialDialogStack()
        {
            string mainView = ReadSource("CtrDxEditor.Shared", "Views", "MainView.axaml");
            string codeBehind = ReadSource("CtrDxEditor.Shared", "Controls", "HelpButton.axaml.cs");

            Assert.Contains("IsMultipleDialogsEnabled=\"True\"", mainView, StringComparison.Ordinal);
            Assert.Contains("_ = await DialogHost.Show(dialog);", codeBehind, StringComparison.Ordinal);
            Assert.DoesNotContain("dialog.ShowAsync()", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Stacked help inserts a backdrop visual between parent and help popup hosts.</summary>
        [Fact]
        public void StackedHelpUsesSamePanelBackdrop()
        {
            string backdrop = ReadSource("CtrDxEditor.Shared", "Controls", "DialogBackdrop.axaml");
            string backdropCode = ReadSource("CtrDxEditor.Shared", "Controls", "DialogBackdrop.axaml.cs");
            string levelSettings = ReadSource("CtrDxEditor.Shared", "Views", "LevelSettingsDialog.axaml");
            string helpButton = ReadSource("CtrDxEditor.Shared", "Controls", "HelpButton.axaml.cs");

            Assert.Contains("DialogHostOverlayBackgroundMixinBrush", backdrop, StringComparison.Ordinal);
            Assert.Contains("IsHitTestVisible=\"True\"", backdrop, StringComparison.Ordinal);
            Assert.Contains("protected override Size MeasureOverride(Size availableSize)", backdropCode,
                StringComparison.Ordinal);
            Assert.Contains("double.IsFinite(availableSize.Width)", backdropCode,
                StringComparison.Ordinal);
            Assert.Contains("double.IsFinite(availableSize.Height)", backdropCode,
                StringComparison.Ordinal);
            Assert.DoesNotContain("<ctrl:DialogSurface>", levelSettings, StringComparison.Ordinal);
            Assert.Contains("DialogSession? parentSession = DialogHost.GetDialogSession(null);", helpButton,
                StringComparison.Ordinal);
            Assert.Contains("DialogBackdrop? backdrop = DialogBackdrop.InsertAfter(parentSession?.Host);",
                helpButton, StringComparison.Ordinal);
            Assert.Contains("parentHost?.GetVisualParent() is not Panel root", backdropCode,
                StringComparison.Ordinal);
            Assert.Contains("root.Children.Insert(parentIndex + 1, backdrop);", backdropCode,
                StringComparison.Ordinal);
            Assert.DoesNotContain("DialogHost.Show(backdrop)", helpButton,
                StringComparison.Ordinal);
            Assert.Contains("_ = await DialogHost.Show(dialog);", helpButton, StringComparison.Ordinal);
            Assert.Contains("finally", helpButton, StringComparison.Ordinal);
            Assert.Contains("backdrop?.Detach();", helpButton, StringComparison.Ordinal);
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

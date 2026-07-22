using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests the palette's placement confirmation, which exists because a palette tap drops the object
    /// at the level centre — behind the drawer in the compact shell, so nothing visibly happens.
    /// </summary>
    public class PalettePlacementFeedbackTests
    {
        /// <summary>The flag is observable, so the row can re-render when it changes.</summary>
        [Fact]
        public void PlacedFlagIsObservable()
        {
            string vm = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "ViewModels", "PaletteItemViewModel.cs"));
            int flag = vm.IndexOf("public bool JustPlaced", StringComparison.Ordinal);

            Assert.True(flag >= 0, "PaletteItemViewModel has no JustPlaced flag.");
            Assert.Contains("set => SetProperty(ref field, value);", vm.AsSpan(flag), StringComparison.Ordinal);
        }

        /// <summary>Touch records the pressed item, which it previously skipped on its early return.</summary>
        /// <remarks>
        /// Touch is the case the confirmation exists for, and the touch press returns before the mouse
        /// path stores the item — so without this the tap has nothing to confirm against.
        /// </remarks>
        [Fact]
        public void TouchPressRecordsTheItem()
        {
            string controller = ReadController();
            int assign = controller.IndexOf("_pendingItem = button.DataContext as PaletteItemViewModel;", StringComparison.Ordinal);
            int touchReturn = controller.IndexOf("if (touch)", StringComparison.Ordinal);

            Assert.True(assign >= 0, "The pressed item is not recorded.");
            Assert.True(touchReturn > assign, "The item must be recorded before the touch early return.");
        }

        /// <summary>Both placement paths confirm, and only when the placement actually happened.</summary>
        [Fact]
        public void BothPlacementPathsConfirm()
        {
            string controller = ReadController();

            Assert.Contains("if (canvas.AddAtCenter(element))", controller, StringComparison.Ordinal);
            Assert.Contains("if (canvas.DropElement(element, onCanvas))", controller, StringComparison.Ordinal);
            // One confirmation inside each success branch.
            Assert.Equal(2, CountOccurrences(controller, "ConfirmPlacement();"));
        }

        /// <summary>One item is confirmed at a time, so the cue always points at the last row tapped.</summary>
        [Fact]
        public void ConfirmationMovesRatherThanAccumulates()
        {
            string controller = ReadController();
            int confirm = controller.IndexOf("private void ConfirmPlacement()", StringComparison.Ordinal);

            Assert.True(confirm >= 0);
            Assert.Contains("previous.JustPlaced = false;", controller.AsSpan(confirm), StringComparison.Ordinal);
        }

        /// <summary>
        /// Cancelling a gesture clears the pending item but never the confirmation.
        /// </summary>
        /// <remarks>
        /// <c>Cancel</c> runs at the end of every release, immediately after a successful placement has
        /// confirmed. Clearing the confirmation there would blank it in the same frame it was set.
        /// </remarks>
        [Fact]
        public void CancelDoesNotClearTheConfirmation()
        {
            string controller = ReadController();
            int cancel = controller.IndexOf("public void Cancel()", StringComparison.Ordinal);

            Assert.True(cancel >= 0);
            string body = controller[cancel..];
            Assert.Contains("_pendingItem = null;", body, StringComparison.Ordinal);
            Assert.DoesNotContain("_placedItem = null;", body, StringComparison.Ordinal);
        }

        /// <summary>The confirmation is timed, not permanent, and reverts on its own.</summary>
        [Fact]
        public void ConfirmationExpiresOnATimer()
        {
            string controller = ReadController();

            Assert.Contains("TimeSpan.FromMilliseconds(900)", controller, StringComparison.Ordinal);
            Assert.Contains("DispatcherTimer", controller, StringComparison.Ordinal);
            Assert.Contains("private void ClearPlacementFeedback()", controller, StringComparison.Ordinal);
        }

        /// <summary>The confirmed row swaps its icon for a check and tints its background.</summary>
        [Fact]
        public void ConfirmedRowSwapsIconAndTints()
        {
            string palette = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteView.axaml"));

            Assert.Contains("Classes.placed=\"{Binding JustPlaced}\"", palette, StringComparison.Ordinal);
            Assert.Contains("Kind=\"CheckCircle\"", palette, StringComparison.Ordinal);
            // The whole icon stack hides, so the dark-theme overlay cannot show through the check.
            Assert.Contains("IsVisible=\"{Binding !JustPlaced}\"", palette, StringComparison.Ordinal);
        }

        /// <summary>The tint targets the template part so Fluent's own states cannot override it.</summary>
        [Fact]
        public void PlacedTintTargetsTheTemplatePart()
        {
            string styles = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Styles", "EditorStyles.axaml"));

            Assert.Contains(
                "Selector=\"Button.placed /template/ ContentPresenter#PART_ContentPresenter\"",
                styles,
                StringComparison.Ordinal);
        }

        private static string ReadController()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteDragController.cs"));
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            return haystack.Split(needle, StringSplitOptions.None).Length - 1;
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

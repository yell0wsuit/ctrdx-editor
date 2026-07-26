using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the compact edit bar's markup and visibility wiring.</summary>
    public class CompactEditBarTests
    {
        /// <summary>The bar exists and carries the five selection/clipboard commands.</summary>
        [Fact]
        public void EditBarCarriesClipboardAndSelectionCommands()
        {
            string view = SourceText("MainView.axaml");

            Assert.Contains("x:Name=\"CompactEditBar\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"Cut_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"Copy_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"Paste_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"CompactProperties_Click\"", view, StringComparison.Ordinal);
        }

        /// <summary>The gear opens the standalone Properties panel rather than the combined Layers panel.</summary>
        [Fact]
        public void GearHostsOnlyPropertiesPanel()
        {
            string layout = SourceText("MainView.Layout.cs");
            int handler = layout.IndexOf("private void CompactProperties_Click", StringComparison.Ordinal);
            int nextHandler = layout.IndexOf("private Control? HostedDrawerPanel", handler, StringComparison.Ordinal);

            Assert.True(handler >= 0 && nextHandler > handler);
            string block = layout[handler..nextHandler];
            Assert.Contains("FindControl<PropertyPanel>(\"PropertiesPanel\")", block, StringComparison.Ordinal);
            Assert.DoesNotContain("LayersPanel", block, StringComparison.Ordinal);
            Assert.Contains("ShowPanelInDrawer(properties);", block, StringComparison.Ordinal);
        }

        /// <summary>
        /// Delete belongs to the edit bar wherever there is one: it is selection-gated like the rest of
        /// that group, and freeing its rail slot is what makes room for the hamburger.
        /// </summary>
        /// <remarks>
        /// The button is declared on the rail because the expanded layout has no edit bar to carry it, but
        /// the compact rail must still hide it - hence the check on the trim rather than on the markup.
        /// </remarks>
        [Fact]
        public void DeleteMovedFromRailToEditBar()
        {
            string view = SourceText("MainView.axaml");
            int bar = view.IndexOf("x:Name=\"CompactEditBar\"", StringComparison.Ordinal);

            Assert.True(bar >= 0);
            Assert.Contains("Click=\"Delete_Click\"", view[bar..], StringComparison.Ordinal);

            string layout = SourceText("MainView.Layout.cs");
            int trim = layout.IndexOf("private void ApplyRailActionSet(bool full)", StringComparison.Ordinal);
            Assert.True(trim >= 0);
            int delete = layout.IndexOf("\"RailDeleteButton\"", trim, StringComparison.Ordinal);
            Assert.True(delete > trim);
            // Visible only when the rail is not the full compact one, i.e. only where no edit bar exists.
            Assert.Contains(
                "delete.IsVisible = !full;",
                layout.AsSpan(delete),
                StringComparison.Ordinal);
        }

        /// <summary>The rail declares eight actions and its narrow-width alignment keeps them clear of the hamburger.</summary>
        /// <remarks>
        /// Seven are up at a time: Help and Delete are mutually exclusive, one per layout mode.
        /// </remarks>
        [Fact]
        public void RailHasEightActionButtons()
        {
            string view = SourceText("MainView.axaml");
            int rail = view.IndexOf("x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf("x:Name=\"CompactEditBar\"", StringComparison.Ordinal);

            Assert.True(rail >= 0 && railEnd > rail);
            Assert.Equal(8, CountOccurrences(view[rail..railEnd], "Classes=\"railAction\""));
        }

        /// <summary>
        /// The bar shows when there is something to act on OR something to paste. Selection alone is the
        /// wrong predicate: CanPaste is gated on the clipboard, so a selection-only bar would hide Paste
        /// exactly when the canvas has just been deselected.
        /// </summary>
        [Fact]
        public void EditBarVisibilityCoversClipboardWithoutSelection()
        {
            string layout = SourceText("MainView.Layout.cs");

            Assert.Contains("UpdateCompactEditBarVisibility", layout, StringComparison.Ordinal);
            Assert.Contains("CanCutSelection: true } or EditorViewModel { CanPaste: true }", layout, StringComparison.Ordinal);
        }

        /// <summary>Clipboard and selection changes refresh the bar.</summary>
        [Fact]
        public void ClipboardAndSelectionChangesRefreshTheBar()
        {
            string view = SourceText("MainView.axaml.cs");
            int handler = view.IndexOf("private void ViewModel_PropertyChanged", StringComparison.Ordinal);

            Assert.True(handler >= 0);
            Assert.Contains("nameof(EditorViewModel.CanPaste)", view.AsSpan(handler), StringComparison.Ordinal);
            Assert.Contains("UpdateCompactEditBarVisibility()", view.AsSpan(handler), StringComparison.Ordinal);
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = haystack.IndexOf(needle, StringComparison.Ordinal);
            while (index >= 0)
            {
                count++;
                index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
            }

            return count;
        }

        private static string SourceText(string file)
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

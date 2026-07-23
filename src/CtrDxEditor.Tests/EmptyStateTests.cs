using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the no-document empty state's markup and wiring.</summary>
    public class EmptyStateTests
    {
        /// <summary>The control carries a mark, both labels, both buttons and the drop hint.</summary>
        [Fact]
        public void EmptyStateCarriesItsCopyAndCommands()
        {
            string view = SourceText("EmptyStateView.axaml");

            Assert.Contains("Kind=\"FilePlusOutline\"", view, StringComparison.Ordinal);
            Assert.Contains("EmptyState.Title", view, StringComparison.Ordinal);
            Assert.Contains("EmptyState.Subtitle", view, StringComparison.Ordinal);
            Assert.Contains("EmptyState.NewLevel", view, StringComparison.Ordinal);
            Assert.Contains("EmptyState.OpenLevel", view, StringComparison.Ordinal);
            Assert.Contains("EmptyState.DropHint", view, StringComparison.Ordinal);
        }

        /// <summary>The host shows it only while no document is open.</summary>
        [Fact]
        public void EmptyStateIsBoundToTheAbsenceOfADocument()
        {
            string view = SourceText("MainView.axaml");

            Assert.Contains("x:Name=\"EmptyState\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding !HasDocument}\"", view, StringComparison.Ordinal);
        }

        /// <summary>
        /// It lives in the canvas column, not the compact overlay band.
        /// </summary>
        /// <remarks>
        /// The compact chrome sits in the outer grid at ZIndex 40-62. Parenting the empty state inside the
        /// canvas column keeps it underneath all of it without an explicit ZIndex, so the hamburger stays
        /// tappable on the start screen - which is the only way to reach New and Open from there.
        /// </remarks>
        [Fact]
        public void EmptyStateSitsBelowTheCompactChrome()
        {
            string view = SourceText("MainView.axaml");
            int empty = view.IndexOf("x:Name=\"EmptyState\"", StringComparison.Ordinal);
            int sheet = view.IndexOf("x:Name=\"CompactSheet\"", StringComparison.Ordinal);
            int columns = view.IndexOf("x:Name=\"ExpandedColumns\"", StringComparison.Ordinal);

            Assert.True(columns >= 0 && empty > columns);
            Assert.True(sheet > empty);
            Assert.DoesNotContain("ZIndex", view[empty..(empty + 400)], StringComparison.Ordinal);
        }

        /// <summary>
        /// The control reaches its host through callbacks, the way LevelCanvas already does.
        /// </summary>
        [Fact]
        public void EmptyStateReachesTheHostThroughCallbacks()
        {
            string host = SourceText("MainView.axaml.cs");

            Assert.Contains("emptyState.NewRequested", host, StringComparison.Ordinal);
            Assert.Contains("emptyState.OpenRequested", host, StringComparison.Ordinal);
        }

        /// <summary>The control never walks the tree to find MainView.</summary>
        [Fact]
        public void EmptyStateDoesNotWalkTheVisualTree()
        {
            string code = SourceText("EmptyStateView.axaml.cs");

            Assert.DoesNotContain("FindAncestor", code, StringComparison.Ordinal);
            Assert.DoesNotContain("GetVisualAncestors", code, StringComparison.Ordinal);
        }

        /// <summary>
        /// The drop hint is hidden in compact mode.
        /// </summary>
        /// <remarks>
        /// Drag-and-drop is wired on the TopLevel, so it works on desktop and in a desktop browser but is
        /// meaningless on a phone, which has nothing to drag from. Gated on layout mode rather than a
        /// platform capability: layout already governs every other touch affordance. The accepted cost is
        /// that a narrow desktop window loses the hint despite supporting the gesture.
        /// </remarks>
        [Fact]
        public void DropHintIsHiddenInCompactMode()
        {
            string layout = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "emptyState.ShowDropHint = _layoutMode != LayoutMode.Compact;",
                layout,
                StringComparison.Ordinal);
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

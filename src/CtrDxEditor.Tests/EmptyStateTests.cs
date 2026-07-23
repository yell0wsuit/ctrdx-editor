using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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

        /// <summary>Both command labels are centred within their buttons.</summary>
        [Fact]
        public void EmptyStateCentersBothButtonLabelsVertically()
        {
            string view = SourceText("EmptyStateView.axaml");

            Assert.Equal(
                2,
                view.Split("VerticalContentAlignment=\"Center\"", StringSplitOptions.None).Length - 1);
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
        /// The no-document surface replaces the editor workspace but remains below compact chrome.
        /// </summary>
        [Fact]
        public void EmptyStateReplacesTheEditorWorkspaceBelowCompactChrome()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            XElement columns = NamedElement(markup, "ExpandedColumns");
            XElement empty = NamedElement(markup, "EmptyState");
            XElement sheet = NamedElement(markup, "CompactSheet");

            Assert.Same(columns.Parent, empty.Parent);
            Assert.Same(columns.Parent, sheet.Parent);
            Assert.Same(empty, columns.ElementsAfterSelf().First());
            Assert.Same(sheet, empty.ElementsAfterSelf().First());
            Assert.Equal("{Binding HasDocument}", columns.Attribute("IsVisible")?.Value);
            Assert.Equal("{Binding !HasDocument}", empty.Attribute("IsVisible")?.Value);
            Assert.DoesNotContain(
                empty.Attributes(),
                attribute => attribute.Name.LocalName == "ZIndex");
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

        private static XElement NamedElement(XDocument markup, string name)
        {
            return markup.Descendants().Single(element =>
                element.Attributes().Any(attribute =>
                    attribute.Name.LocalName == "Name" && attribute.Value == name));
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

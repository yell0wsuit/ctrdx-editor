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

        /// <summary>The usage guide is a localized third action beneath the New and Open row.</summary>
        [Fact]
        public void EmptyStateOffersTheUsageGuideBelowDocumentActions()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "EmptyStateView.axaml"));
            XElement openButton = markup.Descendants().Single(element =>
                element.Attribute("Click")?.Value == "Open_Click");
            XElement guideButton = markup.Descendants().Single(element =>
                element.Attribute("Click")?.Value == "UsageGuide_Click");

            Assert.Same(openButton.Parent, guideButton.Parent);
            Assert.Same(guideButton, openButton.ElementsAfterSelf().First());
            Assert.Equal(
                "{loc:Tr Menu.Help.UsageGuide}",
                guideButton.Attribute("Content")?.Value);
        }

        /// <summary>All three actions share one grid so their left and right edges remain aligned.</summary>
        [Fact]
        public void EmptyStateActionsShareAlignedGrid()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "EmptyStateView.axaml"));
            XElement newButton = markup.Descendants().Single(element =>
                element.Attribute("Click")?.Value == "New_Click");
            XElement openButton = markup.Descendants().Single(element =>
                element.Attribute("Click")?.Value == "Open_Click");
            XElement guideButton = markup.Descendants().Single(element =>
                element.Attribute("Click")?.Value == "UsageGuide_Click");

            Assert.Same(newButton.Parent, openButton.Parent);
            Assert.Same(newButton.Parent, guideButton.Parent);
            Assert.Equal("*,8,*", newButton.Parent?.Attribute("ColumnDefinitions")?.Value);
            Assert.Equal("3", guideButton.Attributes().Single(attribute =>
                attribute.Name.LocalName == "Grid.ColumnSpan").Value);
        }

        /// <summary>All command labels are centred within their buttons.</summary>
        [Fact]
        public void EmptyStateCentersButtonLabelsVertically()
        {
            string view = SourceText("EmptyStateView.axaml");

            Assert.Equal(
                3,
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
            Assert.Contains("emptyState.UsageGuideRequested", host, StringComparison.Ordinal);
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
        /// The drop hint is shown on the desktop heads only.
        /// </summary>
        /// <remarks>
        /// Drag-and-drop needs a local file to drag from, which macOS, Windows and Linux have and the
        /// browser head's phones and tablets do not. Gated on the platform rather than layout mode so a
        /// narrow desktop window keeps the hint for a gesture it still supports; the accepted cost is that
        /// a desktop browser loses it despite the TopLevel handlers working there.
        /// </remarks>
        [Fact]
        public void DropHintIsShownOnDesktopOnly()
        {
            string layout = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "emptyState.ShowDropHint = !OperatingSystem.IsBrowser();",
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

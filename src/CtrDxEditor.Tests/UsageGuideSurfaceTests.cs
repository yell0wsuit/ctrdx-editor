using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards the shared Usage Guide surface and its two platform hosts.</summary>
    public class UsageGuideSurfaceTests
    {
        /// <summary>The guide exposes the approved navigation and discovery controls.</summary>
        [Fact]
        public void SharedSurfaceHasNavigationSearchAndContentsDrawer()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains("x:Name=\"GuideSplitView\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"SidebarButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"BackButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"ForwardButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"HomeButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"GuideSearchBox\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"TableOfContents\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"ArticleScroll\"", view, StringComparison.Ordinal);
        }

        /// <summary>Every structured guide block has an explicit rendering template.</summary>
        /// <param name="blockType">Guide block type name expected in a typed data template.</param>
        [Theory]
        [InlineData("GuideParagraph")]
        [InlineData("GuideHeading")]
        [InlineData("GuideSteps")]
        [InlineData("GuideCallout")]
        [InlineData("GuideShortcutTable")]
        [InlineData("GuideScreenshot")]
        public void SharedSurfaceRendersEveryBlockType(string blockType)
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains($"DataType=\"guide:{blockType}\"", view, StringComparison.Ordinal);
        }

        /// <summary>Screenshot blocks bind both the future image and the named fallback placeholder.</summary>
        [Fact]
        public void ScreenshotTemplateSupportsImageAndPlaceholder()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains("Source=\"{Binding Image}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowImage}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowPlaceholder}\"", view, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding SuggestedFileName}\"", view, StringComparison.Ordinal);
        }

        /// <summary>The search field stretches responsively while preserving a comfortable desktop cap.</summary>
        [Fact]
        public void SearchBoxUsesResponsiveToolbarWidth()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement search = NamedElement(markup, "GuideSearchBox");

            Assert.Null(search.Attribute("Width"));
            Assert.Equal("360", search.Attribute("MaxWidth")?.Value);
            Assert.Equal("Stretch", search.Attribute("HorizontalAlignment")?.Value);
            Assert.Equal("0,0,12,0", search.Attribute("Padding")?.Value);
        }

        /// <summary>The search icon has left breathing room and a short, explicit gap before the placeholder.</summary>
        [Fact]
        public void SearchIconAndPlaceholderUseBalancedInsets()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement search = NamedElement(markup, "GuideSearchBox");
            XElement icon = search.Descendants().Single(element =>
                element.Name.LocalName == "MaterialIcon");

            Assert.Equal("10,0,6,0", icon.Attribute("Margin")?.Value);
            Assert.Equal("0,0,12,0", search.Attribute("Padding")?.Value);
        }

        /// <summary>The guide toolbar moves search onto a second row at narrow widths.</summary>
        [Fact]
        public void SearchBoxReflowsAtNarrowWidths()
        {
            string code = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml.cs"));

            Assert.Contains("UsageGuideLayout.UsesStackedToolbar(Bounds.Width)", code, StringComparison.Ordinal);
            Assert.Contains("Grid.SetRow(searchBox, stacked ? 1 : 0);", code, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumn(searchBox, stacked ? 0 : 5);", code, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumnSpan(searchBox, stacked ? 6 : 1);", code, StringComparison.Ordinal);
        }

        /// <summary>Tip, note, and warning callouts use distinct theme-aware semantic palettes.</summary>
        [Fact]
        public void CalloutTemplateUsesSemanticStyles()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains("Classes.tip=\"{Binding IsTip}\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.note=\"{Binding IsNote}\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.warning=\"{Binding IsWarning}\"", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.SuccessLow", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.SuccessText", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.PrimaryLow", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.Primary", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.WarningLow", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.Warning", view, StringComparison.Ordinal);
        }

        /// <summary>Desktop and browser hosts reuse one guide surface.</summary>
        [Fact]
        public void DesktopAndDialogHostsReuseSharedSurface()
        {
            string window = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideWindow.axaml"));
            string dialog = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideDialog.axaml"));

            Assert.Contains("<views:UsageGuideView", window, StringComparison.Ordinal);
            Assert.Contains("<views:UsageGuideView", dialog, StringComparison.Ordinal);
        }

        /// <summary>The desktop guide can shrink to a compact but still usable reading surface.</summary>
        [Fact]
        public void DesktopWindowAllowsCompactResizing()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideWindow.axaml"));
            XElement window = markup.Root!;

            Assert.Equal("360", window.Attribute("MinWidth")?.Value);
            Assert.Equal("300", window.Attribute("MinHeight")?.Value);
        }

        /// <summary>Finds an element carrying the requested XAML name.</summary>
        /// <param name="markup">Parsed Avalonia markup.</param>
        /// <param name="name">Value of the XAML <c>Name</c> attribute.</param>
        /// <returns>The single matching element.</returns>
        private static XElement NamedElement(XDocument markup, string name)
        {
            return markup.Descendants().Single(element =>
                element.Attributes().Any(attribute =>
                    attribute.Name.LocalName == "Name" && attribute.Value == name));
        }

        /// <summary>Locates a source file relative to the repository's <c>src</c> directory.</summary>
        /// <param name="parts">Path components beneath <c>src</c>.</param>
        /// <returns>Absolute path to the requested source file.</returns>
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

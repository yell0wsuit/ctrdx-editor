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
        public void SharedSurfaceHasNavigationSearchContentsAndResults()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains("x:Name=\"GuideSplitView\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"SidebarButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"BackButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"ForwardButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"HomeButton\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"GuideSearchBox\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"TableOfContents\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"SearchResultsScroll\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"SearchResults\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"ArticleScroll\"", view, StringComparison.Ordinal);
        }

        /// <summary>Search has a dedicated page and never filters the table of contents.</summary>
        [Fact]
        public void SearchResultsAreIndependentFromTableOfContents()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement contents = NamedElement(markup, "TableOfContents");
            XElement results = NamedElement(markup, "SearchResults");

            Assert.Equal("{Binding TocRows}", contents.Attribute("ItemsSource")?.Value);
            Assert.Equal("{Binding SearchResults}", results.Attribute("ItemsSource")?.Value);
        }

        /// <summary>
        /// Result cards are activated, not selected, so the results list carries no selection state
        /// that could disagree with the page or swallow a repeat click.
        /// </summary>
        [Fact]
        public void SearchResultsActivateByClickRatherThanSelection()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement results = NamedElement(markup, "SearchResults");

            Assert.Equal("ItemsControl", results.Name.LocalName);
            Assert.Null(results.Attribute("SelectionChanged"));

            XElement card = Assert.Single(
                results.Descendants(),
                element => element.Name.LocalName == "Button");
            Assert.Equal("SearchResult_Click", card.Attribute("Click")?.Value);
            Assert.Equal("{Binding Id}", card.Attribute("Tag")?.Value);
        }

        /// <summary>
        /// Contents rows are activated and highlighted from data, with no selection state of their own.
        /// </summary>
        /// <remarks>
        /// A selection model is a second copy of "where am I" living in the control, and it only tracks
        /// the view model while the containers holding it are realized - which a closed drawer's rows are
        /// not. Binding the highlight to each row's own flag leaves nothing to fall out of step.
        /// </remarks>
        [Fact]
        public void TableOfContentsHighlightIsBoundPerRowRatherThanSelected()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement contents = NamedElement(markup, "TableOfContents");
            string codeBehind = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml.cs"));

            Assert.Equal("ItemsControl", contents.Name.LocalName);
            Assert.Null(contents.Attribute("SelectedItem"));
            Assert.Null(contents.Attribute("SelectionChanged"));

            XElement row = Assert.Single(
                contents.Descendants(),
                element => element.Name.LocalName == "Button");
            Assert.Equal("TableOfContentsRow_Click", row.Attribute("Click")?.Value);
            Assert.Equal("{Binding Id}", row.Attribute("Tag")?.Value);
            Assert.Equal("{Binding IsActive}", row.Attribute("Classes.reading")?.Value);

            Assert.DoesNotContain("SelectedItem", codeBehind, StringComparison.Ordinal);
            Assert.DoesNotContain("SelectedTocArticle", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Starting a search stands the compact drawer down.</summary>
        /// <remarks>
        /// The drawer overlays the results page rather than displacing it, so leaving it open would put
        /// the pane on top of the cards a reader is trying to tap.
        /// </remarks>
        [Fact]
        public void StartingASearchClosesTheCompactDrawer()
        {
            string codeBehind = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml.cs"));

            int handler = codeBehind.IndexOf(
                "private void ViewModel_PropertyChanged",
                StringComparison.Ordinal);
            int nextMember = codeBehind.IndexOf(
                "private void ApplyAdaptiveLayout",
                handler,
                StringComparison.Ordinal);
            Assert.True(handler >= 0 && nextMember > handler);

            ReadOnlySpan<char> body = codeBehind.AsSpan(handler, nextMember - handler);
            Assert.Contains("IsSearchActive", body, StringComparison.Ordinal);
            Assert.Contains("CloseCompactSidebar();", body, StringComparison.Ordinal);
        }

        /// <summary>
        /// The row being read is marked without filling behind its text, and stays off the shared
        /// latched class.
        /// </summary>
        /// <remarks>
        /// Every line keeps the panel's own foreground, so no contrast pairing has to be maintained
        /// for the marker. Filling the row and writing over it is what produced blue on blue at 3.8:1,
        /// and <c>Button.active</c> - the latched idiom for a single short label - writes exactly that
        /// accent foreground, so the contents row must not carry it.
        /// </remarks>
        [Fact]
        public void ReadRowIsMarkedWithoutFillingBehindItsText()
        {
            string markup = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.DoesNotContain("Classes.active", markup, StringComparison.Ordinal);

            int style = markup.IndexOf(
                "Button.tocRow.reading /template/ ContentPresenter#PART_ContentPresenter",
                StringComparison.Ordinal);
            int nextStyle = markup.IndexOf("</Style>", style, StringComparison.Ordinal);
            Assert.True(style >= 0 && nextStyle > style);

            // A Background setter here would put the text back on a coloured fill.
            ReadOnlySpan<char> body = markup.AsSpan(style, nextStyle - style);
            Assert.Contains("BorderBrush", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Background", body, StringComparison.Ordinal);

            // The rail rides the border every row already reserves, so marking one shifts no layout.
            int baseStyle = markup.IndexOf("<Style Selector=\"Button.tocRow\">", StringComparison.Ordinal);
            int baseEnd = markup.IndexOf("</Style>", baseStyle, StringComparison.Ordinal);
            Assert.Contains(
                "BorderThickness\" Value=\"3,0,0,0\"",
                markup.AsSpan(baseStyle, baseEnd - baseStyle),
                StringComparison.Ordinal);
        }

        /// <summary>The drawer toggle describes both of its states through the native pane events.</summary>
        [Fact]
        public void SidebarToggleLabelFollowsBothPaneStates()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement splitView = NamedElement(markup, "GuideSplitView");
            string codeBehind = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml.cs"));

            Assert.Equal("GuideSplitView_PaneOpened", splitView.Attribute("PaneOpened")?.Value);
            Assert.Equal("GuideSplitView_PaneClosed", splitView.Attribute("PaneClosed")?.Value);
            Assert.Contains("Guide.Navigation.HideContents", codeBehind, StringComparison.Ordinal);
            Assert.Contains("Guide.Navigation.ShowContents", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Matched-term rendering is confined to the dedicated result template.</summary>
        [Fact]
        public void MatchHighlightingAppearsOnlyInSearchResults()
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement results = NamedElement(markup, "SearchResults");

            Assert.Equal(3, results.Descendants().Count(element =>
                element.Name.LocalName == "SearchHighlightTextBlock"));
            Assert.DoesNotContain(
                markup.Descendants().Where(element => !results.DescendantsAndSelf().Contains(element)),
                element => element.Name.LocalName == "SearchHighlightTextBlock");
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

        /// <summary>Only article body-copy templates opt into inline emphasis rendering.</summary>
        /// <param name="blockType">Guide block whose body copy may contain emphasis markers.</param>
        /// <param name="binding">Markup binding expected on the rich text control.</param>
        [Theory]
        [InlineData("GuideParagraph", "{Binding Text}")]
        [InlineData("GuideSteps", "{Binding .}")]
        [InlineData("GuideCallout", "{Binding Text}")]
        [InlineData("GuideScreenshot", "{Binding Caption}")]
        public void BodyTextTemplatesRenderInlineEmphasis(string blockType, string binding)
        {
            XDocument markup = XDocument.Load(
                SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));
            XElement template = markup.Descendants().Single(element =>
                element.Name.LocalName == "DataTemplate"
                && element.Attribute("DataType")?.Value == $"guide:{blockType}");

            Assert.Contains(
                template.Descendants(),
                element => element.Name.LocalName == "GuideTextBlock"
                           && element.Attribute("Markup")?.Value == binding);
        }

        /// <summary>The renderer uses the bundled Inter family so italic runs resolve to real italic faces.</summary>
        [Fact]
        public void BodyTextRendererUsesBundledInterFamily()
        {
            string control = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Controls", "GuideTextBlock.cs"));
            string project = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "CtrDxEditor.Shared.csproj"));

            Assert.Contains(
                "avares://CtrDxEditor.Shared/Assets/Fonts/Inter/#Inter",
                control,
                StringComparison.Ordinal);
            Assert.Contains(
                @"resources\fonts\Inter-*.ttf",
                project,
                StringComparison.Ordinal);
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

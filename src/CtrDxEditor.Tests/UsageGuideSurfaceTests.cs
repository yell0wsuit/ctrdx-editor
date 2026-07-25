using System;
using System.IO;

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

            Assert.Contains("Source=\"{Binding Source}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowImage}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding ShowPlaceholder}\"", view, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding SuggestedFileName}\"", view, StringComparison.Ordinal);
        }

        /// <summary>The guide search field stays compact, right-aligned, and vertically centered.</summary>
        [Fact]
        public void SearchBoxUsesCompactToolbarLayout()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "UsageGuideView.axaml"));

            Assert.Contains("x:Name=\"GuideSearchBox\" Grid.Column=\"5\"", view, StringComparison.Ordinal);
            Assert.Contains("Width=\"300\" Height=\"36\"", view, StringComparison.Ordinal);
            Assert.Contains("HorizontalAlignment=\"Right\" VerticalAlignment=\"Center\"", view, StringComparison.Ordinal);
            Assert.Contains("VerticalContentAlignment=\"Center\" Padding=\"10,0,12,0\"", view, StringComparison.Ordinal);
            Assert.Contains("Margin=\"0,0,8,0\"", view, StringComparison.Ordinal);
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

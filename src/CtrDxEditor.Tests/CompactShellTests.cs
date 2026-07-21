using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the compact shell's wiring, which is impractical to drive through headless layout.</summary>
    public class CompactShellTests
    {
        /// <summary>Layout mode is recomputed whenever the view's bounds change.</summary>
        [Fact]
        public void LayoutModeTracksBoundsChanges()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("BoundsProperty", layout, StringComparison.Ordinal);
            Assert.Contains("AdaptiveLayout.ModeFor", layout, StringComparison.Ordinal);
        }

        /// <summary>
        /// The mode is applied only when it actually changes, so a resize does not reparent panels on
        /// every layout pass.
        /// </summary>
        [Fact]
        public void LayoutModeAppliesOnlyOnChange()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("if (mode == _layoutMode)", layout, StringComparison.Ordinal);
        }

        /// <summary>The constructor wires layout tracking, so the mode is live from startup.</summary>
        [Fact]
        public void ConstructorWiresLayoutMode()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("WireLayoutMode();", view, StringComparison.Ordinal);
        }

        /// <summary>The compact shell hosts panels in a bottom drawer over a full-bleed canvas.</summary>
        [Fact]
        public void CompactShellUsesBottomDrawer()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("<DrawerPage", view, StringComparison.Ordinal);
            Assert.Contains("DrawerPlacement=\"Bottom\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"Shell\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"DrawerHost\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"CompactTabs\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"CompactRail\"", view, StringComparison.Ordinal);
        }

        /// <summary>The tab bar stays above the drawer so an open sheet cannot block panel switching.</summary>
        [Fact]
        public void CompactTabsOverlayTheDrawer()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            int drawerEnd = view.IndexOf("</DrawerPage>", StringComparison.Ordinal);
            int tabs = view.IndexOf("x:Name=\"CompactTabs\"", StringComparison.Ordinal);

            Assert.True(drawerEnd >= 0 && tabs > drawerEnd);
        }

        /// <summary>
        /// Panels are moved between the expanded grid and the drawer, never duplicated: two controls named
        /// LayersTree in one name scope is a XAML load error and would break every handler that finds it.
        /// </summary>
        [Fact]
        public void PanelsAreReparentedNotDuplicated()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("Children.Remove", layout, StringComparison.Ordinal);
            Assert.Contains("ShowPanelInDrawer", layout, StringComparison.Ordinal);
            // One palette and one layers panel in the markup.
            Assert.Equal(1, CountOccurrences(view, "<views:PaletteView"));
            Assert.Equal(1, CountOccurrences(view, "x:Name=\"LayersPanel\""));
        }

        /// <summary>Compact chrome is padded by the safe area so controls clear the notch and home indicator.</summary>
        [Fact]
        public void CompactChromeAppliesSafeAreaPadding()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("SafeAreaProbe.Read(this)", layout, StringComparison.Ordinal);
        }

        /// <summary>Rotating a compact device refreshes insets even though its layout mode is unchanged.</summary>
        [Fact]
        public void CompactBoundsChangesRefreshSafeAreaPadding()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("if (mode == LayoutMode.Compact)", layout, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(layout, "ApplyCompactSafeAreaPadding();"));
        }

        /// <summary>Expanded mode keeps the three-column grid and hides all compact chrome.</summary>
        [Fact]
        public void ExpandedModeRestoresTheColumns()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("LayoutMode.Expanded", layout, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumn", layout, StringComparison.Ordinal);
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

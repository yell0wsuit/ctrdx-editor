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

            Assert.Contains("x:Name=\"CompactSheet\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"DrawerHost\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"CompactTabs\"", view, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"CompactRail\"", view, StringComparison.Ordinal);
        }

        /// <summary>
        /// The sheet is a plain overlay, not a <c>DrawerPage</c>.
        /// </summary>
        /// <remarks>
        /// <c>DrawerPage</c> was tried first and reverted: it is a page-level navigation shell whose
        /// template carries its own title bars and pane toggle buttons, which rendered over the editor in
        /// expanded mode and could not be reliably suppressed from outside the template. Suppressing them
        /// needs selectors that name internal parts, which are not part of Avalonia's public contract and
        /// silently stop matching when a part's type changes.
        /// </remarks>
        [Fact]
        public void ShellDoesNotUseDrawerPage()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            // The element, not the word: the markup comment explaining why it was dropped must survive.
            Assert.DoesNotContain("<DrawerPage", view, StringComparison.Ordinal);
            Assert.DoesNotContain("PART_", view, StringComparison.Ordinal);
        }

        /// <summary>The compact undo/redo rail hugs its controls instead of covering the canvas in landscape.</summary>
        [Fact]
        public void CompactRailDoesNotStretchAcrossTheCanvas()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf('>', railStart);

            Assert.True(railStart >= 0 && railEnd > railStart);
            Assert.Contains(
                "HorizontalAlignment=\"Left\"",
                view.AsSpan(railStart, railEnd - railStart),
                StringComparison.Ordinal);
        }

        /// <summary>The tab bar stays above the drawer so an open sheet cannot block panel switching.</summary>
        [Fact]
        public void CompactTabsOverlayTheDrawer()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            int sheet = view.IndexOf("x:Name=\"CompactSheet\"", StringComparison.Ordinal);
            int tabs = view.IndexOf("x:Name=\"CompactTabs\"", StringComparison.Ordinal);

            // Later siblings paint on top, so the tab bar must be declared after the sheet.
            Assert.True(sheet >= 0 && tabs > sheet);
        }

        /// <summary>Drawer content reserves the overlaid tab bar and horizontal safe areas.</summary>
        [Fact]
        public void CompactDrawerContentClearsTheTabBar()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "sheet.Margin = new Thickness(insets.Left, 0, insets.Right, tabs.Bounds.Height);",
                layout,
                StringComparison.Ordinal);
        }

        /// <summary>A tab-height change refreshes drawer clearance after safe-area padding is laid out.</summary>
        [Fact]
        public void CompactTabBoundsChangesRefreshDrawerClearance()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("tabs.PropertyChanged +=", layout, StringComparison.Ordinal);
            Assert.Contains("if (e.Property == BoundsProperty && _layoutMode == LayoutMode.Compact)", layout, StringComparison.Ordinal);
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

        /// <summary>A left-floating rail consumes only the safe-area edges that can overlap it.</summary>
        [Fact]
        public void CompactRailUsesLeftAndTopSafeAreaOnly()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "rail.Padding = new Thickness(insets.Left, insets.Top, 0, 0);",
                layout,
                StringComparison.Ordinal);
            Assert.Contains(
                "tabs.Padding = new Thickness(insets.Left, 0, insets.Right, insets.Bottom);",
                layout,
                StringComparison.Ordinal);
        }

        /// <summary>Rotating a compact device refreshes insets even though its layout mode is unchanged.</summary>
        [Fact]
        public void CompactBoundsChangesRefreshSafeAreaPadding()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            int unchangedModeBranch = layout.IndexOf("if (mode == _layoutMode)", StringComparison.Ordinal);
            int applyChangedMode = layout.IndexOf("ApplyLayoutMode(mode);", unchangedModeBranch, StringComparison.Ordinal);

            Assert.True(unchangedModeBranch >= 0 && applyChangedMode > unchangedModeBranch);
            Assert.Contains(
                "ApplyCompactSafeAreaPadding();",
                layout.AsSpan(unchangedModeBranch, applyChangedMode - unchangedModeBranch),
                StringComparison.Ordinal);
        }

        /// <summary>
        /// All compact chrome is gated on an open document, and hiding it closes the drawer.
        /// </summary>
        /// <remarks>
        /// With no document both panels are empty and there is nothing to undo, so the tab bar would open a
        /// blank sheet and the rail would float two permanently disabled buttons over the start screen.
        /// </remarks>
        [Fact]
        public void CompactChromeRequiresAnOpenDocument()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("bool show = _layoutMode == LayoutMode.Compact", layout, StringComparison.Ordinal);
            Assert.Contains("EditorViewModel { HasDocument: true }", layout, StringComparison.Ordinal);
            Assert.Contains("tabs.IsVisible = show;", layout, StringComparison.Ordinal);
            Assert.Contains("rail.IsVisible = show;", layout, StringComparison.Ordinal);
            // The rail must be gated through the same predicate, not left on for the whole compact mode.
            Assert.DoesNotContain("rail.IsVisible = compact;", layout, StringComparison.Ordinal);
            // Closing a document must take the open sheet down with the tabs that raised it.
            int gate = layout.IndexOf("tabs.IsVisible = show;", StringComparison.Ordinal);
            Assert.Contains(
                "sheet.IsVisible = false;",
                layout.AsSpan(gate),
                StringComparison.Ordinal);
        }

        /// <summary>Opening or closing a document re-evaluates the chrome without a layout-mode change.</summary>
        [Fact]
        public void DocumentChangesRefreshCompactChrome()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("nameof(EditorViewModel.HasDocument)", view, StringComparison.Ordinal);
            Assert.Contains("UpdateCompactChromeVisibility();", view, StringComparison.Ordinal);
        }

        /// <summary>The compact palette tab reuses the existing "Objects" panel label.</summary>
        [Fact]
        public void CompactTabsUseTheObjectsLabel()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("{loc:Tr Panel.Objects}", view, StringComparison.Ordinal);
            Assert.DoesNotContain("Panel.Palette", view, StringComparison.Ordinal);
        }

        /// <summary>
        /// The rail carries the four actions a touch session cannot otherwise reach quickly, each gated on
        /// the same capability its menu item uses.
        /// </summary>
        /// <remarks>
        /// Delete is on the rail because a touch session has no keyboard Delete key, and Zoom to Fit because
        /// it is the only way back from a pinch that threw the level off-screen. The rail floats over the
        /// canvas, so this list is deliberately short — anything further belongs in the menu bar.
        /// </remarks>
        [Fact]
        public void CompactRailCarriesTheTouchCriticalActions()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf("</Border>", railStart, StringComparison.Ordinal);
            Assert.True(railStart >= 0 && railEnd > railStart);
            string rail = view[railStart..railEnd];

            Assert.Contains("Click=\"Undo_Click\"", rail, StringComparison.Ordinal);
            Assert.Contains("Click=\"Redo_Click\"", rail, StringComparison.Ordinal);
            Assert.Contains("Click=\"Delete_Click\"", rail, StringComparison.Ordinal);
            Assert.Contains("Click=\"ZoomFit_Click\"", rail, StringComparison.Ordinal);

            // Each action is gated on the capability its menu item uses, so the rail cannot invoke a command
            // the menu considers unavailable.
            Assert.Contains("IsEnabled=\"{Binding CanDeleteSelection}\"", rail, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding HasDocument}\"", rail, StringComparison.Ordinal);

            // Four buttons: the rail overlays the canvas and portrait has no room for a fifth.
            Assert.Equal(4, CountOccurrences(rail, "<Button "));
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

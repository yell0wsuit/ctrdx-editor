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

        /// <summary>
        /// The compact rail is centred and hugs its controls instead of covering the canvas in landscape.
        /// </summary>
        [Fact]
        public void CompactRailIsCentredAndDoesNotStretchAcrossTheCanvas()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf('>', railStart);

            Assert.True(railStart >= 0 && railEnd > railStart);
            // Centre, not Stretch: stretching would paint a bar over the full width of the level.
            Assert.Contains(
                "HorizontalAlignment=\"Center\"",
                view.AsSpan(railStart, railEnd - railStart),
                StringComparison.Ordinal);
        }

        /// <summary>The rail is centred on the canvas, not on the window.</summary>
        /// <remarks>
        /// It hangs off the canvas column rather than the shell row, so "centred" follows the canvas in
        /// both modes: the column widens to the full width in compact, and in expanded it excludes the
        /// palette and inspector, which are not the same width - hosting the rail a level up left it 40px
        /// off-centre on a landscape tablet.
        /// </remarks>
        [Fact]
        public void RailIsCentredOnTheCanvasColumn()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int canvas = view.IndexOf("<render:LevelCanvas x:Name=\"Canvas\"", StringComparison.Ordinal);
            int columnEnd = view.IndexOf("<Border Grid.Column=\"3\"", StringComparison.Ordinal);
            int rail = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);

            Assert.True(canvas >= 0 && columnEnd > canvas);
            // Inside the canvas column, and after the canvas so it draws and hit-tests above it.
            Assert.True(rail > canvas && rail < columnEnd);
            // A row assignment would put it back on the shell grid, off-centre again.
            int railEnd = view.IndexOf('>', rail);
            Assert.DoesNotContain(
                "Grid.Row=",
                view.AsSpan(rail, railEnd - rail),
                StringComparison.Ordinal);
        }

        /// <summary>Toasts move out of the compact chrome's way.</summary>
        /// <remarks>
        /// Compact spends its bottom edge on the tab bar and edit bar, its top-left on the hamburger, and
        /// its top centre on the rail, so the top-right is the only corner not already claimed. Expanded
        /// has nothing but canvas in the bottom-right and keeps it. Applied on every call rather than at
        /// construction: the host is created lazily and may predate the first layout pass.
        /// </remarks>
        [Fact]
        public void ToastsAvoidTheCompactChrome()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            int host = view.IndexOf("private WindowNotificationManager? Notifications()", StringComparison.Ordinal);

            Assert.True(host >= 0);
            string body = view[host..];

            Assert.Contains("NotificationPosition.TopRight", body, StringComparison.Ordinal);
            Assert.Contains("NotificationPosition.BottomRight", body, StringComparison.Ordinal);
            Assert.Contains("_layoutMode == LayoutMode.Compact", body, StringComparison.Ordinal);
            // Not baked into the constructor, where the mode is not known yet.
            Assert.DoesNotContain(
                "Position = NotificationPosition.BottomRight,",
                view,
                StringComparison.Ordinal);
        }

        /// <summary>Compact bottom chrome occupies layout rows so it cannot cover the canvas.</summary>
        [Fact]
        public void CompactBottomChromeReservesCanvasSpace()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains(
                "<Grid x:Name=\"EditorSurface\" RowDefinitions=\"*,Auto,Auto\">",
                view,
                StringComparison.Ordinal);
            Assert.Contains(
                "<Border x:Name=\"CompactEditBar\" IsVisible=\"False\" Grid.Row=\"1\"",
                view,
                StringComparison.Ordinal);
            Assert.Contains(
                "<Border x:Name=\"CompactTabs\" IsVisible=\"False\" Grid.Row=\"2\"",
                view,
                StringComparison.Ordinal);
        }

        /// <summary>The compact drawer stays in the canvas row and respects horizontal safe areas.</summary>
        [Fact]
        public void CompactDrawerUsesCanvasRow()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "<Border x:Name=\"CompactSheet\" IsVisible=\"False\" Grid.Row=\"0\"",
                view,
                StringComparison.Ordinal);
            Assert.Contains(
                "sheet.Margin = new Thickness(insets.Left, 0, insets.Right, 0);",
                layout,
                StringComparison.Ordinal);
        }

        /// <summary>Reserved rows remove any dependency on the tab bar's measured height.</summary>
        [Fact]
        public void CompactRowsDoNotDependOnMeasuredTabHeight()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.DoesNotContain("tabs.Bounds.Height", layout, StringComparison.Ordinal);
        }

        /// <summary>A laid-out canvas preserves its center when its bounds change.</summary>
        [Fact]
        public void CanvasBoundsChangesResizeTheViewport()
        {
            string canvas = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.cs"));

            Assert.Contains("ViewNavigation.ResizeViewport(", canvas, StringComparison.Ordinal);
            Assert.Contains("change.GetOldValue<Rect>()", canvas, StringComparison.Ordinal);
            Assert.Contains("change.GetNewValue<Rect>()", canvas, StringComparison.Ordinal);
        }

        /// <summary>
        /// Panels are moved between the expanded inspector and the drawer, never duplicated: duplicate
        /// controls in one name scope would be a XAML load error and break name-based handlers.
        /// </summary>
        [Fact]
        public void PanelsAreReparentedNotDuplicated()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("Children.Remove", layout, StringComparison.Ordinal);
            Assert.Contains("ShowPanelInDrawer", layout, StringComparison.Ordinal);
            // One instance of each independently hosted panel in the markup.
            Assert.Equal(1, CountOccurrences(view, "<views:PaletteView"));
            Assert.Equal(1, CountOccurrences(view, "x:Name=\"InspectorPanel\""));
            Assert.Equal(1, CountOccurrences(view, "x:Name=\"LayersPanel\""));
            Assert.Equal(1, CountOccurrences(view, "x:Name=\"PropertiesPanel\""));
            Assert.Contains("RestoreToInspector(inspector, layers, properties);", layout, StringComparison.Ordinal);
        }

        /// <summary>The Layers tab opens only layer controls, leaving Properties to the contextual gear.</summary>
        [Fact]
        public void LayersTabHostsOnlyLayersPanel()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            int handler = layout.IndexOf("private void CompactLayersTab_Click", StringComparison.Ordinal);
            int nextHandler = layout.IndexOf("private void CompactProperties_Click", handler, StringComparison.Ordinal);

            Assert.True(handler >= 0 && nextHandler > handler);
            string block = layout[handler..nextHandler];
            Assert.Contains("FindControl<Grid>(\"LayersPanel\")", block, StringComparison.Ordinal);
            Assert.DoesNotContain("PropertiesPanel", block, StringComparison.Ordinal);
        }

        /// <summary>Compact chrome is padded by the safe area so controls clear the notch and home indicator.</summary>
        [Fact]
        public void CompactChromeAppliesSafeAreaPadding()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("SafeAreaProbe.Read(this)", layout, StringComparison.Ordinal);
        }

        /// <summary>A centred rail consumes only the safe-area edge that can overlap it.</summary>
        /// <remarks>
        /// Centring puts the rail clear of both side notches, and a one-sided horizontal padding would
        /// shift it off centre rather than protect it.
        /// </remarks>
        [Fact]
        public void CompactRailUsesTopSafeAreaOnly()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains(
                "rail.Padding = new Thickness(0, insets.Top, 0, 0);",
                layout,
                StringComparison.Ordinal);
            Assert.Contains(
                "tabs.Padding = new Thickness(insets.Left, 0, insets.Right, 0);",
                layout,
                StringComparison.Ordinal);
            Assert.Contains(
                "double tabBottomPadding = Math.Max(12, insets.Bottom);",
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

            Assert.Contains("bool show = compact && hasDocument;", layout, StringComparison.Ordinal);
            Assert.Contains("EditorViewModel { HasDocument: true }", layout, StringComparison.Ordinal);
            Assert.Contains("tabs.IsVisible = show;", layout, StringComparison.Ordinal);
            // The rail may outlive the compact shell on touch, but never the document it acts on.
            Assert.Contains(
                "bool showRail = hasDocument && (compact || _touchSeen);",
                layout,
                StringComparison.Ordinal);
            Assert.Contains("rail.IsVisible = showRail;", layout, StringComparison.Ordinal);
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
        /// The rail carries the three always-applicable actions a touch session cannot otherwise reach
        /// quickly, each gated on
        /// the same capability its menu item uses.
        /// </summary>
        /// <remarks>
        /// Delete moved to the contextual edit bar with the other selection-gated commands. Zoom to Fit
        /// stays because it is the only way back from a pinch that threw the level off-screen. The rail
        /// floats over the canvas, so this list is deliberately short.
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
            Assert.Contains("Click=\"ZoomFit_Click\"", rail, StringComparison.Ordinal);

            // Each action is gated on the capability its menu item uses, so the rail cannot invoke a command
            // the menu considers unavailable.
            Assert.Contains("IsEnabled=\"{Binding HasDocument}\"", rail, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanDeleteSelection}\"", rail, StringComparison.Ordinal);

            // Eight declared, seven up at a time: the three document actions, the mode pair, rotation snap,
            // and either Help or Delete depending on the layout.
            Assert.Equal(8, CountOccurrences(rail, "<Button "));
        }

        /// <summary>The usage guide follows Zoom to Fit in its own divided rail group.</summary>
        [Fact]
        public void CompactRailPlacesUsageGuideAfterZoomFitDivider()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf("</Border>", railStart, StringComparison.Ordinal);
            Assert.True(railStart >= 0 && railEnd > railStart);
            string rail = view[railStart..railEnd];

            int zoomFit = rail.IndexOf("Click=\"ZoomFit_Click\"", StringComparison.Ordinal);
            int divider = rail.IndexOf("x:Name=\"RailHelpDivider\"", StringComparison.Ordinal);
            int usageGuide = rail.IndexOf("Click=\"UsageGuide_Click\"", StringComparison.Ordinal);

            Assert.True(zoomFit >= 0 && divider > zoomFit && usageGuide > divider);
            Assert.Contains("ToolTip.Tip=\"{loc:Tr Menu.Help.UsageGuide}\"", rail, StringComparison.Ordinal);
            Assert.Contains("Kind=\"HelpCircleOutline\"", rail, StringComparison.Ordinal);
        }

        /// <summary>A touch session keeps the rail after the layout widens past the breakpoint.</summary>
        /// <remarks>
        /// Every iPad crosses the 1024 breakpoint when rotated to landscape - 1133 on a mini, 1180 on an
        /// Air - and lands in the expanded shell, which assumes a menu bar and shortcuts a bare tablet has
        /// no way to reach. Latched on the first touch contact rather than measured per pointer, so a
        /// tablet in a keyboard case does not flicker the rail between a finger and its trackpad.
        /// </remarks>
        [Fact]
        public void ExpandedModeKeepsTheRailForATouchSession()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("private bool _touchSeen;", layout, StringComparison.Ordinal);
            Assert.Contains("e.Pointer.Type != PointerType.Touch", layout, StringComparison.Ordinal);
            // Tunnelled: the control under the finger handles the press itself.
            Assert.Contains(
                "AddHandler(PointerPressedEvent, Shell_PointerPressed, RoutingStrategies.Tunnel);",
                layout,
                StringComparison.Ordinal);
            // Latched, so it is never cleared once set.
            Assert.Equal(1, CountOccurrences(layout, "_touchSeen = true;"));
            Assert.DoesNotContain("_touchSeen = false;", layout, StringComparison.Ordinal);
        }

        /// <summary>The expanded rail trades the menu-friendly actions for Delete.</summary>
        /// <remarks>
        /// Rotation snap and the usage guide are set-and-forget, so they stay in the Edit and Help menus
        /// rather than occluding canvas. Zoom to Fit is kept: it is the way back from a pinch that threw
        /// the level off-screen, which a landscape tablet can do exactly as easily as a phone. Delete moves
        /// the other way - the expanded layout has no edit bar, so the rail is its only touch home.
        /// </remarks>
        [Fact]
        public void ExpandedRailDropsTheMenuFriendlyActions()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("ApplyRailActionSet(compact);", layout, StringComparison.Ordinal);
            int trim = layout.IndexOf("private void ApplyRailActionSet(bool full)", StringComparison.Ordinal);
            Assert.True(trim >= 0);
            int trimEnd = layout.IndexOf("\n        /// <summary>", trim, StringComparison.Ordinal);
            Assert.True(trimEnd > trim);
            string body = layout[trim..trimEnd];

            Assert.Contains("snap.IsVisible = full;", body, StringComparison.Ordinal);
            Assert.Contains("help.IsVisible = full;", body, StringComparison.Ordinal);
            Assert.Contains("delete.IsVisible = !full;", body, StringComparison.Ordinal);
            // The mode pair, Undo, Redo and Zoom to Fit are never trimmed.
            Assert.DoesNotContain("\"EditModeButton\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("\"PanModeButton\"", body, StringComparison.Ordinal);
        }

        /// <summary>Expanded mode keeps the three-column grid and hides all compact chrome.</summary>
        [Fact]
        public void ExpandedModeRestoresTheColumns()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("LayoutMode.Expanded", layout, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumn", layout, StringComparison.Ordinal);
        }

        /// <summary>The rail carries a latched mode pair alongside the one-shot actions.</summary>
        /// <remarks>
        /// The pair is declared first and divided off from the actions: a latched mode and a one-shot
        /// action read differently, and interleaving them makes the row ambiguous at a glance.
        /// </remarks>
        [Fact]
        public void CompactRailCarriesTheModePair()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf("</Border>", railStart, StringComparison.Ordinal);
            Assert.True(railStart >= 0 && railEnd > railStart);
            string rail = view[railStart..railEnd];

            Assert.Contains("x:Name=\"EditModeButton\"", rail, StringComparison.Ordinal);
            Assert.Contains("x:Name=\"PanModeButton\"", rail, StringComparison.Ordinal);
            // The pair leads the row, before the first action.
            Assert.True(
                rail.IndexOf("x:Name=\"EditModeButton\"", StringComparison.Ordinal)
                    < rail.IndexOf("Click=\"Undo_Click\"", StringComparison.Ordinal));
        }

        /// <summary>Every rail button uses the tightened padding.</summary>
        [Fact]
        public void CompactRailButtonsUseTheTightenedPadding()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            int railStart = view.IndexOf("<Border x:Name=\"CompactRail\"", StringComparison.Ordinal);
            int railEnd = view.IndexOf("</Border>", railStart, StringComparison.Ordinal);
            string rail = view[railStart..railEnd];

            Assert.Equal(CountOccurrences(rail, "<Button "), CountOccurrences(rail, "Classes=\"railAction\""));
        }

        /// <summary>The rail exposes an independent, visibly latched rotation-snap toggle.</summary>
        [Fact]
        public void CompactRailCarriesRotationSnapToggle()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            string viewModel = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs"));

            Assert.Contains("x:Name=\"RotationSnapButton\"", view, StringComparison.Ordinal);
            Assert.Contains("Click=\"RotationSnapToggle_Click\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.active=\"{Binding RotationSnapEnabled}\"", view, StringComparison.Ordinal);
            Assert.Contains("RotationSnapEnabled=\"{Binding RotationSnapEnabled}\"", view, StringComparison.Ordinal);
            string commands = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));
            Assert.Contains("vm.RotationSnapEnabled = !vm.RotationSnapEnabled;", commands, StringComparison.Ordinal);
            Assert.Contains("partial bool RotationSnapEnabled", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("vm.SnapEnabled = !vm.SnapEnabled;", layout, StringComparison.Ordinal);
        }

        /// <summary>Seven rail buttons shift right on narrow phones so they do not collide with the menu.</summary>
        [Fact]
        public void CompactRailClearsMenuAtNarrowWidths()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("Bounds.Width is > 0 and < 360", layout, StringComparison.Ordinal);
            Assert.Contains("HorizontalAlignment.Right", layout, StringComparison.Ordinal);
            Assert.Contains("HorizontalAlignment.Center", layout, StringComparison.Ordinal);
        }

        /// <summary>The mode is applied to the canvas and latched on one button in a single place.</summary>
        [Fact]
        public void RailModeButtonsLatchThroughOneHelper()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("private void SetInteractionMode(CanvasInteractionMode mode)", layout, StringComparison.Ordinal);
            Assert.Contains("_canvas.InteractionMode = mode;", layout, StringComparison.Ordinal);
            Assert.Contains("edit.Classes.Set(\"active\", mode == CanvasInteractionMode.Edit);", layout, StringComparison.Ordinal);
            Assert.Contains("pan.Classes.Set(\"active\", mode == CanvasInteractionMode.Pan);", layout, StringComparison.Ordinal);
        }

        /// <summary>
        /// Hiding the rail resets the mode, so pan mode can never outlive its only exit.
        /// </summary>
        /// <remarks>
        /// One reset site, keyed on the rail's own predicate rather than on the compact shell, so it covers
        /// every way the rail disappears — closing the document, and a mouse session widening past the
        /// breakpoint — without firing for the touch session that widens and keeps it.
        /// </remarks>
        [Fact]
        public void HidingTheRailResetsInteractionMode()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            int gate = layout.IndexOf("rail.IsVisible = showRail;", StringComparison.Ordinal);

            Assert.True(gate >= 0);
            Assert.Contains(
                "if (!showRail)",
                layout.AsSpan(gate),
                StringComparison.Ordinal);
            Assert.Contains(
                "SetInteractionMode(CanvasInteractionMode.Edit);",
                layout.AsSpan(gate),
                StringComparison.Ordinal);
        }

        /// <summary>The latched style targets the template part so Fluent's own states cannot win.</summary>
        [Fact]
        public void ActiveStyleTargetsTheTemplatePart()
        {
            string styles = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Styles", "EditorStyles.axaml"));

            Assert.Contains(
                "Selector=\"Button.active /template/ ContentPresenter#PART_ContentPresenter\"",
                styles,
                StringComparison.Ordinal);
        }

        /// <summary>The open drawer's tab is latched, so the tab bar shows which panel is up.</summary>
        /// <remarks>
        /// Identity, not type name: the panels are reparented between the columns and the drawer, and a
        /// reference check cannot go stale the way a name-string match can.
        /// </remarks>
        [Fact]
        public void OpenDrawerLatchesItsTab()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("private void UpdateCompactTabState()", layout, StringComparison.Ordinal);
            Assert.Contains("paletteTab.Classes.Set(\"active\",", layout, StringComparison.Ordinal);
            Assert.Contains("layersTab.Classes.Set(\"active\",", layout, StringComparison.Ordinal);
            Assert.Contains("ReferenceEquals(hosted,", layout, StringComparison.Ordinal);
        }

        /// <summary>A closed sheet latches neither tab, whatever it still holds.</summary>
        /// <remarks>
        /// Closing the drawer leaves the panel parented in <c>DrawerHost</c>, so hosting alone is not
        /// enough — visibility has to be part of the predicate or the tab stays lit over a closed sheet.
        /// </remarks>
        [Fact]
        public void ClosedDrawerLatchesNoTab()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("bool open = sheet.IsVisible;", layout, StringComparison.Ordinal);
            Assert.Contains("open && drawerHost.Children.Count > 0", layout, StringComparison.Ordinal);
        }

        /// <summary>Every path that changes sheet visibility refreshes the tab latch.</summary>
        [Fact]
        public void TabStateRefreshesWhereverTheSheetToggles()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            // Both branches of ShowPanelInDrawer (re-tap toggle and fresh open) plus the chrome gate.
            Assert.True(CountOccurrences(layout, "UpdateCompactTabState();") >= 3);
        }

        /// <summary>The press interceptor is consulted before any other work on a canvas press.</summary>
        /// <remarks>
        /// A callback rather than a tunnel-route handler: this does not depend on Avalonia's ordering
        /// between tunnel handlers and the class handler that raises <c>OnPointerPressed</c>, and it
        /// matches the PlaceAt/SelectionRequested callbacks the canvas already carries.
        /// </remarks>
        [Fact]
        public void CanvasPressInterceptorRunsFirst()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            int intercept = input.IndexOf("PressIntercepted?.Invoke() == true", StringComparison.Ordinal);
            int docCheck = input.IndexOf("LevelDocument? doc = Document;", StringComparison.Ordinal);

            Assert.True(intercept >= 0, "OnPointerPressed does not consult PressIntercepted.");
            Assert.True(docCheck > intercept, "The interceptor must run before anything else.");
        }

        /// <summary>An intercepted press is swallowed whole rather than passed on.</summary>
        [Fact]
        public void InterceptedPressIsSwallowed()
        {
            string input = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
            int intercept = input.IndexOf("PressIntercepted?.Invoke() == true", StringComparison.Ordinal);
            int docCheck = input.IndexOf("LevelDocument? doc = Document;", StringComparison.Ordinal);

            Assert.True(intercept >= 0 && docCheck > intercept);
            string block = input[intercept..docCheck];
            Assert.Contains("e.Handled = true;", block, StringComparison.Ordinal);
            Assert.Contains("return;", block, StringComparison.Ordinal);
        }

        /// <summary>The compact shell wires the interceptor to dismiss an open drawer.</summary>
        [Fact]
        public void CompactShellDismissesTheDrawerOnCanvasPress()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("canvas.PressIntercepted = DismissCompactDrawerOnCanvasPress;", view, StringComparison.Ordinal);
            Assert.Contains("private bool DismissCompactDrawerOnCanvasPress()", layout, StringComparison.Ordinal);
        }

        /// <summary>Only a compact layout with an open sheet intercepts; everything else falls through.</summary>
        /// <remarks>
        /// Returning true unconditionally would make the expanded canvas inert, which is the sort of bug
        /// that only shows up on desktop after the mobile work looks finished.
        /// </remarks>
        [Fact]
        public void DrawerDismissalIsGatedOnAnOpenCompactSheet()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));
            int method = layout.IndexOf("private bool DismissCompactDrawerOnCanvasPress()", StringComparison.Ordinal);

            Assert.True(method >= 0);
            Assert.Contains(
                "_layoutMode != LayoutMode.Compact",
                layout.AsSpan(method),
                StringComparison.Ordinal);
            Assert.Contains(
                "is not { IsVisible: true } sheet",
                layout.AsSpan(method),
                StringComparison.Ordinal);
        }

        /// <summary>The desktop menu is named so layout code can hide it in compact mode.</summary>
        [Fact]
        public void DesktopMenuIsNamedAndCompactHidden()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("x:Name=\"DesktopMenu\"", view, StringComparison.Ordinal);
            Assert.Contains("desktopMenu.IsVisible = !compact;", layout, StringComparison.Ordinal);
        }

        /// <summary>
        /// The hamburger is not document-gated the way the rail and tabs are: New and Open live in the
        /// drawer, so it must work before any document exists.
        /// </summary>
        [Fact]
        public void HamburgerIsCompactGatedNotDocumentGated()
        {
            string layout = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Layout.cs"));

            Assert.Contains("menuButton.IsVisible = _layoutMode == LayoutMode.Compact;", layout, StringComparison.Ordinal);
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

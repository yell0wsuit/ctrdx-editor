using System;

using Avalonia;
using Avalonia.Controls;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Adaptive layout: tracks the available width and switches the shell between modes.</summary>
    public partial class MainView : UserControl
    {
        private LayoutMode _layoutMode = LayoutMode.Expanded;

        /// <summary>
        /// Starts tracking the view's width and applying the matching layout mode.
        /// </summary>
        /// <remarks>
        /// Driven by the control's own bounds rather than a screen or platform query, so narrowing a
        /// desktop window exercises the compact shell exactly as a phone does.
        /// </remarks>
        private void WireLayoutMode()
        {
            PropertyChanged += (_, e) =>
            {
                if (e.Property == BoundsProperty)
                {
                    UpdateLayoutMode();
                }
            };

            UpdateLayoutMode();
        }

        /// <summary>Recomputes the mode from the current width and applies it if it changed.</summary>
        private void UpdateLayoutMode()
        {
            LayoutMode mode = AdaptiveLayout.ModeFor(Bounds.Width);
            if (mode == _layoutMode)
            {
                if (mode == LayoutMode.Compact)
                {
                    // Orientation and browser-chrome changes can alter CSS env() insets while the width
                    // remains compact. Refresh padding without reparenting either panel.
                    ApplyCompactSafeAreaPadding();
                }

                // Bounds change on every layout pass; reparenting panels each time would be both wasteful
                // and destructive to scroll position.
                return;
            }

            _layoutMode = mode;
            ApplyLayoutMode(mode);
        }

        /// <summary>Rearranges the shell for the given mode.</summary>
        /// <param name="mode">The layout mode to apply.</param>
        private void ApplyLayoutMode(LayoutMode mode)
        {
            if (this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Panel>("DrawerHost") is not { } drawerHost
                || this.FindControl<Grid>("ExpandedColumns") is not { } columns
                || this.FindControl<Grid>("InspectorPanel") is not { } inspector
                || this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactTabs") is not { } tabs
                || this.FindControl<PaletteView>("Palette") is not { } palette
                || this.FindControl<Grid>("LayersPanel") is not { } layers
                || this.FindControl<PropertyPanel>("PropertiesPanel") is not { } properties)
            {
                return;
            }

            bool compact = mode == LayoutMode.Compact;
            UpdateCompactChromeVisibility();

            if (compact)
            {
                // Pull every drawer panel out of its expanded parent so each can be hosted independently.
                MoveOutOfColumns(columns, palette);
                MoveOutOfInspector(inspector, layers);
                MoveOutOfInspector(inspector, properties);
                columns.ColumnDefinitions = ColumnDefinitions.Parse("0,0,*,0,0");

                ApplyCompactSafeAreaPadding();
            }
            else
            {
                bool focusCanvas = IsCommandDrawerOpen;
                SetCommandDrawerOpen(false, restoreFocus: false);
                if (focusCanvas)
                {
                    _ = _canvas.Focus();
                }

                sheet.IsVisible = false;
                drawerHost.Children.Clear();

                RestoreToColumn(columns, palette, 0);
                RestoreToInspector(inspector, layers, properties);
                columns.ColumnDefinitions = ColumnDefinitions.Parse("200,1,*,1,280");

                sheet.Margin = new Thickness(0);
                rail.Padding = new Thickness(0);
                tabs.Padding = new Thickness(0);
            }
        }

        /// <summary>
        /// Shows the compact tab bar and undo/redo rail only while a document is open, and closes the
        /// drawer with them.
        /// </summary>
        /// <remarks>
        /// With no document both panels are empty and there is nothing to undo, so the chrome would float
        /// two blank-sheet buttons and two permanently disabled ones over the start screen. Set in code
        /// rather than bound in XAML because the layout mode is also code-driven, and a local
        /// <c>IsVisible</c> value would permanently outrank a binding on the same property.
        /// </remarks>
        private void UpdateCompactChromeVisibility()
        {
            if (this.FindControl<Border>("CompactTabs") is not { } tabs
                || this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Panel>("DrawerHost") is not { } drawerHost)
            {
                return;
            }

            bool show = _layoutMode == LayoutMode.Compact
                && DataContext is EditorViewModel { HasDocument: true };
            tabs.IsVisible = show;
            rail.IsVisible = show;
            // The hamburger is not document-gated like the rail and tabs: New and Open live in the
            // drawer, so it has to be reachable from the start screen.
            if (this.FindControl<Button>("CompactMenuButton") is { } menuButton)
            {
                menuButton.IsVisible = _layoutMode == LayoutMode.Compact;
            }

            // Dropping needs a file to drag from, which a phone does not have.
            if (this.FindControl<EmptyStateView>("EmptyState") is { } emptyState)
            {
                emptyState.ShowDropHint = _layoutMode != LayoutMode.Compact;
            }

            bool compact = _layoutMode == LayoutMode.Compact;
            if (this.FindControl<Menu>("DesktopMenu") is { } desktopMenu)
            {
                // Docked, so hiding it gives the row back to the canvas rather than leaving a gap.
                desktopMenu.IsVisible = !compact;
            }

            if (!show)
            {
                sheet.IsVisible = false;
                drawerHost.Children.Clear();
                // The rail is the only way out of pan mode, so a latched mode must not outlive it. This one
                // site covers both ways the rail disappears: widening to the expanded layout, and closing
                // the document.
                SetInteractionMode(CanvasInteractionMode.Edit);
            }

            UpdateCompactTabState();
            UpdateCompactEditBarVisibility();
        }

        /// <summary>Refreshes compact chrome padding from the platform's current safe-area insets.</summary>
        private void ApplyCompactSafeAreaPadding()
        {
            if (this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactTabs") is not { } tabs
                || this.FindControl<Border>("CompactEditBar") is not { } editBar
                || this.FindControl<Border>("CompactCommandDrawer") is not { } commandDrawer
                || this.FindControl<Button>("CompactMenuButton") is not { } menuButton
                || this.FindControl<Button>("PaletteTab") is not { } paletteTab
                || this.FindControl<Button>("LayersTab") is not { } layersTab)
            {
                return;
            }

            Thickness insets = SafeAreaProbe.Read(this);
            // The rail is centred, so it already clears both side notches; only the top inset can overlap
            // it. Padding a side would also shift it off centre, since padding is not symmetric.
            rail.Padding = new Thickness(0, insets.Top, 0, 0);
            bool narrowRail = Bounds.Width is > 0 and < 360;
            rail.HorizontalAlignment = narrowRail
                ? Avalonia.Layout.HorizontalAlignment.Right
                : Avalonia.Layout.HorizontalAlignment.Center;
            rail.Margin = narrowRail
                ? new Thickness(0, 0, Math.Max(4, insets.Right), 0)
                : new Thickness(0);
            // Horizontal insets belong to the bar. Bottom clearance belongs inside each button so its
            // hover/active surface continues behind the safe-area strip while the label stays above it.
            tabs.Padding = new Thickness(insets.Left, 0, insets.Right, 0);
            double tabBottomPadding = Math.Max(12, insets.Bottom);
            ApplyCompactTabBottomPadding(paletteTab, tabBottomPadding);
            ApplyCompactTabBottomPadding(layersTab, tabBottomPadding);
            // Its dedicated layout row keeps it above both the canvas and the tab bar.
            editBar.Margin = new Thickness(insets.Left, 0, insets.Right, 0);
            menuButton.Margin = new Thickness(insets.Left, insets.Top, 0, 0);
            commandDrawer.Padding = new Thickness(insets.Left, insets.Top, 0, insets.Bottom);
            // Narrower than a typical nav drawer on purpose: toggles leave it open, and their effect is
            // on the canvas behind it. 260 caps it on tablets; 70% keeps ~96px of canvas at 320.
            commandDrawer.Width = Bounds.Width > 0 ? Math.Min(260, Bounds.Width * 0.70) : 260;
            // The sheet occupies the canvas row, above the reserved compact chrome rows.
            sheet.Margin = new Thickness(insets.Left, 0, insets.Right, 0);
            sheet.Height = CompactSheetHeight();
        }

        /// <summary>Extends a tab surface through the bottom inset while keeping a 48px content row.</summary>
        private static void ApplyCompactTabBottomPadding(Button tab, double bottomPadding)
        {
            tab.MinHeight = 48 + bottomPadding;
            tab.Padding = new Thickness(0, 0, 0, bottomPadding);
        }

        /// <summary>
        /// The sheet's height: a little over half the view, leaving a strip of canvas visible so the level
        /// never disappears entirely behind a panel.
        /// </summary>
        /// <returns>The height in logical pixels, floored so the sheet stays usable on short screens.</returns>
        private double CompactSheetHeight()
        {
            double available = Bounds.Height;
            return available > 0 ? Math.Max(200, available * 0.55) : 320;
        }

        /// <summary>Detaches a panel from the column grid so it can be hosted in the drawer.</summary>
        /// <param name="columns">The three-column grid.</param>
        /// <param name="panel">The panel to detach.</param>
        private static void MoveOutOfColumns(Grid columns, Control panel)
        {
            if (columns.Children.Contains(panel))
            {
                _ = columns.Children.Remove(panel);
            }
        }

        /// <summary>Detaches one compact-sheet panel from the expanded inspector.</summary>
        private static void MoveOutOfInspector(Grid inspector, Control panel)
        {
            if (inspector.Children.Contains(panel))
            {
                _ = inspector.Children.Remove(panel);
            }
        }

        /// <summary>Returns a panel to its column in the expanded grid.</summary>
        /// <param name="columns">The three-column grid.</param>
        /// <param name="panel">The panel to restore.</param>
        /// <param name="column">The grid column it belongs in.</param>
        private static void RestoreToColumn(Grid columns, Control panel, int column)
        {
            if (panel.Parent is Panel current && !ReferenceEquals(current, columns))
            {
                _ = current.Children.Remove(panel);
            }

            if (!columns.Children.Contains(panel))
            {
                Grid.SetColumn(panel, column);
                columns.Children.Add(panel);
            }
        }

        /// <summary>Restores Layers and Properties to their expanded inspector rows.</summary>
        private static void RestoreToInspector(Grid inspector, Control layers, Control properties)
        {
            RestoreToInspectorRow(inspector, layers, 0);
            RestoreToInspectorRow(inspector, properties, 2);
        }

        private static void RestoreToInspectorRow(Grid inspector, Control panel, int row)
        {
            if (panel.Parent is Panel current && !ReferenceEquals(current, inspector))
            {
                _ = current.Children.Remove(panel);
            }

            if (!inspector.Children.Contains(panel))
            {
                Grid.SetRow(panel, row);
                inspector.Children.Add(panel);
            }
        }

        /// <summary>Shows a single panel in the bottom drawer, replacing whatever was there.</summary>
        /// <param name="panel">The panel to host.</param>
        private void ShowPanelInDrawer(Control panel)
        {
            if (this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Panel>("DrawerHost") is not { } drawerHost)
            {
                return;
            }

            if (drawerHost.Children.Contains(panel))
            {
                // Tapping the active tab closes the sheet, giving the canvas back.
                sheet.IsVisible = !sheet.IsVisible;
                UpdateCompactTabState();
                UpdateCompactEditBarVisibility();
                return;
            }

            drawerHost.Children.Clear();
            if (panel.Parent is Panel previous)
            {
                _ = previous.Children.Remove(panel);
            }

            drawerHost.Children.Add(panel);
            sheet.Height = CompactSheetHeight();
            sheet.IsVisible = true;
            UpdateCompactTabState();
            UpdateCompactEditBarVisibility();
        }

        /// <summary>Latches whichever compact tab has its panel up in an open drawer.</summary>
        /// <remarks>
        /// Matched by reference against the named panels rather than by type or name string: both panels
        /// are reparented between the expanded columns and the drawer, and a reference check cannot go
        /// stale when one of them is renamed or wrapped. Visibility is part of the predicate because
        /// closing the sheet leaves its panel parented in <c>DrawerHost</c>.
        /// </remarks>
        private void UpdateCompactTabState()
        {
            if (this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Panel>("DrawerHost") is not { } drawerHost
                || this.FindControl<Button>("PaletteTab") is not { } paletteTab
                || this.FindControl<Button>("LayersTab") is not { } layersTab)
            {
                return;
            }

            bool open = sheet.IsVisible;
            Control? hosted = open && drawerHost.Children.Count > 0 ? drawerHost.Children[0] : null;

            paletteTab.Classes.Set("active",
                hosted is not null && ReferenceEquals(hosted, this.FindControl<PaletteView>("Palette")));
            layersTab.Classes.Set("active",
                hosted is not null && ReferenceEquals(hosted, this.FindControl<Grid>("LayersPanel")));
        }

        /// <summary>Closes an open compact drawer when the canvas is pressed.</summary>
        /// <returns>
        /// True when a drawer was open and has been closed, meaning the canvas should ignore the press;
        /// false whenever the press should reach the canvas as usual.
        /// </returns>
        private bool DismissCompactDrawerOnCanvasPress()
        {
            if (_layoutMode != LayoutMode.Compact)
            {
                return false;
            }

            if (IsCommandDrawerOpen)
            {
                SetCommandDrawerOpen(false);
                return true;
            }

            if (this.FindControl<Border>("CompactSheet") is not { IsVisible: true } sheet)
            {
                return false;
            }

            sheet.IsVisible = false;
            UpdateCompactTabState();
            UpdateCompactEditBarVisibility();
            return true;
        }

        /// <summary>Raises the palette in the compact drawer.</summary>
        private void CompactPaletteTab_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<PaletteView>("Palette") is { } palette)
            {
                ShowPanelInDrawer(palette);
            }
        }

        /// <summary>Raises the layers panel in the compact drawer.</summary>
        private void CompactLayersTab_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<Grid>("LayersPanel") is { } layers)
            {
                ShowPanelInDrawer(layers);
            }
        }

        /// <summary>Raises the selected object's standalone Properties panel.</summary>
        private void CompactProperties_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.FindControl<PropertyPanel>("PropertiesPanel") is not { } properties
                || this.FindControl<Border>("CompactSheet") is not { } sheet)
            {
                return;
            }

            // ShowPanelInDrawer toggles when the panel is already hosted; Properties must always open.
            if (!sheet.IsVisible || !ReferenceEquals(HostedDrawerPanel(), properties))
            {
                ShowPanelInDrawer(properties);
            }
        }

        /// <summary>The panel currently hosted in the compact sheet, or null when it is closed.</summary>
        private Control? HostedDrawerPanel()
        {
            return this.FindControl<Border>("CompactSheet") is { IsVisible: true }
                && this.FindControl<Panel>("DrawerHost") is { Children.Count: > 0 } host
                ? host.Children[0]
                : null;
        }

        /// <summary>Shows the edit bar while there is a selection to act on or a clipboard to paste.</summary>
        /// <remarks>
        /// The predicate is deliberately wider than "something is selected": <c>CanPaste</c> is gated on
        /// the clipboard, not the selection, so a selection-only bar would hide Paste at exactly the
        /// moment it is wanted - copy an object, tap empty canvas to deselect, paste. The bar also stands
        /// down while either overlay is up, since both cover the strip it occupies.
        /// </remarks>
        private void UpdateCompactEditBarVisibility()
        {
            if (this.FindControl<Border>("CompactEditBar") is not { } bar)
            {
                return;
            }

            bool sheetOpen = this.FindControl<Border>("CompactSheet") is { IsVisible: true };
            bool actionable = DataContext is EditorViewModel { CanCutSelection: true } or EditorViewModel { CanPaste: true };

            bar.IsVisible = _layoutMode == LayoutMode.Compact && !sheetOpen && !IsCommandDrawerOpen && actionable;
        }

        /// <summary>Switches the canvas to edit mode from the compact rail.</summary>
        /// <param name="sender">The rail button (unused).</param>
        /// <param name="e">Routed event data (unused).</param>
        private void CompactEditMode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SetInteractionMode(CanvasInteractionMode.Edit);
        }

        /// <summary>Switches the canvas to pan mode from the compact rail.</summary>
        /// <param name="sender">The rail button (unused).</param>
        /// <param name="e">Routed event data (unused).</param>
        private void CompactPanMode_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SetInteractionMode(CanvasInteractionMode.Pan);
        }

        /// <summary>Applies an interaction mode to the canvas and latches the matching rail button.</summary>
        /// <param name="mode">The mode to apply.</param>
        /// <remarks>
        /// Both the canvas property and the button classes are set here so the two cannot drift: a mode
        /// the canvas is in but no button shows would be indistinguishable from a stuck canvas.
        /// </remarks>
        private void SetInteractionMode(CanvasInteractionMode mode)
        {
            _canvas.InteractionMode = mode;

            if (this.FindControl<Button>("EditModeButton") is { } edit)
            {
                edit.Classes.Set("active", mode == CanvasInteractionMode.Edit);
            }

            if (this.FindControl<Button>("PanModeButton") is { } pan)
            {
                pan.Classes.Set("active", mode == CanvasInteractionMode.Pan);
            }
        }
    }
}

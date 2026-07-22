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

            if (this.FindControl<Border>("CompactTabs") is { } tabs)
            {
                tabs.PropertyChanged += (_, e) =>
                {
                    if (e.Property == BoundsProperty && _layoutMode == LayoutMode.Compact)
                    {
                        // Padding changes the tab bar's measured height; keep drawer content above it.
                        ApplyCompactSafeAreaPadding();
                    }
                };
            }

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
                || this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactTabs") is not { } tabs
                || this.FindControl<PaletteView>("Palette") is not { } palette
                || this.FindControl<Grid>("LayersPanel") is not { } layers)
            {
                return;
            }

            bool compact = mode == LayoutMode.Compact;
            UpdateCompactChromeVisibility();

            if (compact)
            {
                // Pull both panels out of the columns so the canvas spans the full width, and let the canvas
                // column absorb the freed space.
                MoveOutOfColumns(columns, palette);
                MoveOutOfColumns(columns, layers);
                columns.ColumnDefinitions = ColumnDefinitions.Parse("0,0,*,0,0");

                ApplyCompactSafeAreaPadding();
            }
            else
            {
                sheet.IsVisible = false;
                drawerHost.Children.Clear();

                RestoreToColumn(columns, palette, 0);
                RestoreToColumn(columns, layers, 4);
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

            if (!show)
            {
                sheet.IsVisible = false;
                drawerHost.Children.Clear();
                // The rail is the only way out of pan mode, so a latched mode must not outlive it. This one
                // site covers both ways the rail disappears: widening to the expanded layout, and closing
                // the document.
                SetInteractionMode(CanvasInteractionMode.Edit);
            }
        }

        /// <summary>Refreshes compact chrome padding from the platform's current safe-area insets.</summary>
        private void ApplyCompactSafeAreaPadding()
        {
            if (this.FindControl<Border>("CompactSheet") is not { } sheet
                || this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactTabs") is not { } tabs)
            {
                return;
            }

            Thickness insets = SafeAreaProbe.Read(this);
            // The rail is centred, so it already clears both side notches; only the top inset can overlap
            // it. Padding a side would also shift it off centre, since padding is not symmetric.
            rail.Padding = new Thickness(0, insets.Top, 0, 0);
            tabs.Padding = new Thickness(insets.Left, 0, insets.Right, insets.Bottom);
            // The sheet sits directly above the tab bar and inside any side notches.
            sheet.Margin = new Thickness(insets.Left, 0, insets.Right, tabs.Bounds.Height);
            sheet.Height = CompactSheetHeight();
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

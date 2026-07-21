using Avalonia;
using Avalonia.Controls;

using CtrDxEditor.Core.Editing;

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
            if (this.FindControl<DrawerPage>("Shell") is not { } shell
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
            rail.IsVisible = compact;
            tabs.IsVisible = compact;
            shell.DrawerBehavior = compact ? DrawerBehavior.Flyout : DrawerBehavior.Disabled;

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
                shell.IsOpen = false;
                drawerHost.Children.Clear();

                RestoreToColumn(columns, palette, 0);
                RestoreToColumn(columns, layers, 4);
                columns.ColumnDefinitions = ColumnDefinitions.Parse("200,1,*,1,280");

                rail.Padding = new Thickness(0);
                tabs.Padding = new Thickness(0);
            }
        }

        /// <summary>Refreshes compact chrome padding from the platform's current safe-area insets.</summary>
        private void ApplyCompactSafeAreaPadding()
        {
            if (this.FindControl<Border>("CompactRail") is not { } rail
                || this.FindControl<Border>("CompactTabs") is not { } tabs)
            {
                return;
            }

            Thickness insets = SafeAreaProbe.Read(this);
            // The rail hugs the left edge, so a right-side notch cannot overlap it and must not widen it.
            rail.Padding = new Thickness(insets.Left, insets.Top, 0, 0);
            tabs.Padding = new Thickness(insets.Left, 0, insets.Right, insets.Bottom);
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
            if (this.FindControl<DrawerPage>("Shell") is not { } shell
                || this.FindControl<Panel>("DrawerHost") is not { } drawerHost)
            {
                return;
            }

            if (drawerHost.Children.Contains(panel))
            {
                // Tapping the active tab closes the sheet, giving the canvas back.
                shell.IsOpen = !shell.IsOpen;
                return;
            }

            drawerHost.Children.Clear();
            if (panel.Parent is Panel previous)
            {
                _ = previous.Children.Remove(panel);
            }

            drawerHost.Children.Add(panel);
            shell.IsOpen = true;
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
    }
}

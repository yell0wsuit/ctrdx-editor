using System;
using System.Collections.Generic;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;
using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Thin menu/toolbar click handlers that delegate straight to the view model or canvas. Keyboard
    // equivalents live in the KeyDown handler wired up in the constructor.
    public partial class MainView
    {
        private void Undo_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.Undo();
            }
        }

        private void Redo_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.Redo();
            }
        }

        private void Delete_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.DeleteSelected();
                this.FindControl<LevelCanvas>("Canvas")!.InvalidateVisual();
            }
        }

        private void SnapToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.SnapEnabled = !vm.SnapEnabled;
            }
        }

        private void ShowHitboxesToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ShowHitboxes = !vm.ShowHitboxes;
            }
        }

        private void ShowForceFieldsToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ShowForceFields = !vm.ShowForceFields;
            }
        }

        private void ShowMovementPathsToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ShowMovementPaths = !vm.ShowMovementPaths;
            }
        }

        private void AnimationPreviewToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.ToggleAnimationPreviewAll();
            }
        }

        private void ObjectAnimationPreview_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is Button { Tag: LevelObject obj })
            {
                vm.ToggleAnimationPreviewObject(obj);
                e.Handled = true;
            }
        }

        private void ObjectLock_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is Button { Tag: LevelObject obj })
            {
                vm.ToggleLock(obj);
                e.Handled = true;
            }
        }

        private void LayerMoveUp_Click(object? sender, RoutedEventArgs e)
        {
            (DataContext as EditorViewModel)?.MoveActiveLayer(-1);
        }

        private void LayerMoveDown_Click(object? sender, RoutedEventArgs e)
        {
            (DataContext as EditorViewModel)?.MoveActiveLayer(1);
        }

        private void LayerVisibility_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm
                && sender is ToggleButton { Tag: LayerViewModel row } toggle)
            {
                vm.SetLayerHidden(row.Layer, toggle.IsChecked != true);
                e.Handled = true;
            }
        }

        private void ObjectVisibility_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm
                && sender is ToggleButton { Tag: LevelObject obj })
            {
                if (vm.EffectivelyHiddenObjects.Contains(obj))
                {
                    vm.RevealObject(obj);
                }
                else
                {
                    vm.SetObjectHidden(obj, true);
                }
                e.Handled = true;
            }
        }

        private void LayerName_KeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not TextBox { Tag: LayerViewModel row })
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                row.Name = row.Layer.Name;
                _ = _canvas.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                _ = _canvas.Focus();
                e.Handled = true;
            }
        }

        private void LayerName_Commit(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm
                || sender is not TextBox { Tag: LayerViewModel row, Text: { } name }
                || name == row.Layer.Name)
            {
                return;
            }

            if (!vm.RenameLayer(row.Layer, name))
            {
                row.Name = row.Layer.Name;
            }
        }

        private void ObjectContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            if (DataContext is not EditorViewModel vm
                || sender is not ContextMenu { Tag: LevelObject obj } menu)
            {
                e.Cancel = true;
                return;
            }

            List<MenuItem> targets = [];
            foreach (LayerViewModel row in vm.Layers)
            {
                if (ReferenceEquals(obj.Element.Parent, row.Layer.Element))
                {
                    continue;
                }

                MenuItem target = new()
                {
                    Header = row.Name,
                    Tag = new MoveTarget(obj, row.Layer),
                };
                target.Click += MoveObjectToLayer_Click;
                targets.Add(target);
            }

            menu.ItemsSource = new[]
            {
                new MenuItem
                {
                    Header = Localizer.Get("Object.MoveToLayer"),
                    ItemsSource = targets,
                    IsEnabled = targets.Count > 0,
                },
            };
        }

        private void MoveObjectToLayer_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: MoveTarget target })
            {
                vm.MoveObjectToLayer(target.Object, target.Layer);
            }
        }

        private sealed record MoveTarget(LevelObject Object, LevelLayer Layer);

        private TextBox? _layerRenameTarget;
        private static readonly DataFormat<LayerViewModel> LayerDragFormat =
            DataFormat.CreateInProcessFormat<LayerViewModel>("ctrdx-layer-row");
        private LayerViewModel? _dragLayer;
        private PointerPressedEventArgs? _dragTrigger;
        private Point _dragStart;

        private void LayerRow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: LayerViewModel row }
                && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                _dragLayer = row;
                _dragTrigger = e;
                _dragStart = e.GetPosition(null);
            }
        }

        private async void LayerRow_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                ClearPendingLayerDrag();
                return;
            }

            if (_dragLayer is not { } row || _dragTrigger is not { } trigger)
            {
                return;
            }

            Point now = e.GetPosition(null);
            if (Math.Abs(now.X - _dragStart.X) < 4 && Math.Abs(now.Y - _dragStart.Y) < 4)
            {
                return;
            }

            ClearPendingLayerDrag();
            DataTransfer data = new();
            data.Add(DataTransferItem.Create(LayerDragFormat, row));
            _ = await DragDrop.DoDragDropAsync(trigger, data, DragDropEffects.Move);
        }

        private void LayerRow_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            ClearPendingLayerDrag();
        }

        private void LayerRow_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            ClearPendingLayerDrag();
        }

        private void ClearPendingLayerDrag()
        {
            _dragLayer = null;
            _dragTrigger = null;
        }

        private void LayerRow_DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.DataTransfer.Contains(LayerDragFormat)
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void LayerRow_Drop(object? sender, DragEventArgs e)
        {
            if (DataContext is not EditorViewModel vm
                || e.DataTransfer.TryGetValue(LayerDragFormat) is not LayerViewModel source
                || sender is not Control { DataContext: LayerViewModel target }
                || ReferenceEquals(source, target))
            {
                return;
            }

            int targetIndex = vm.Layers.IndexOf(target);
            if (targetIndex >= 0)
            {
                vm.MoveLayerToIndex(source.Layer, targetIndex);
            }
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void LayerContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            if (sender is not ContextMenu { Tag: LayerViewModel row } menu)
            {
                e.Cancel = true;
                return;
            }

            _layerRenameTarget = (menu.PlacementTarget as Visual)?.FindDescendantOfType<TextBox>();

            MenuItem rename = new() { Header = Localizer.Get("Layer.MenuRename") };
            rename.Click += LayerRename_Click;

            MenuItem moveUp = new() { Header = Localizer.Get("Layer.MenuMoveUp"), Tag = row };
            moveUp.Click += LayerMenuMoveUp_Click;

            MenuItem moveDown = new() { Header = Localizer.Get("Layer.MenuMoveDown"), Tag = row };
            moveDown.Click += LayerMenuMoveDown_Click;

            MenuItem lockItem = new()
            {
                Header = Localizer.Get(row.IsLocked ? "Layer.Unlock" : "Layer.Lock"),
                Tag = row,
            };
            lockItem.Click += LayerLockToggle_Click;

            MenuItem hideItem = new()
            {
                Header = Localizer.Get(row.IsVisible ? "Layer.Hide" : "Layer.Show"),
                Tag = row,
            };
            hideItem.Click += LayerHideToggle_Click;

            MenuItem delete = new() { Header = Localizer.Get("Layer.MenuDelete"), Tag = row };
            delete.Click += LayerDelete_Click;

            menu.ItemsSource = new Control[]
            {
                rename,
                new Separator(),
                moveUp,
                moveDown,
                new Separator(),
                lockItem,
                hideItem,
                new Separator(),
                delete,
            };
        }

        private void LayerRename_Click(object? sender, RoutedEventArgs e)
        {
            if (_layerRenameTarget is { } target)
            {
                _ = target.Focus();
                target.SelectAll();
            }
        }

        private void LayerMenuMoveUp_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: LayerViewModel row })
            {
                vm.MoveLayer(row.Layer, -1);
            }
        }

        private void LayerMenuMoveDown_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: LayerViewModel row })
            {
                vm.MoveLayer(row.Layer, 1);
            }
        }

        private void LayerLockToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: LayerViewModel row })
            {
                vm.SetLayerLocked(row.Layer, !vm.IsLayerLocked(row.Layer));
            }
        }

        private void LayerHideToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: LayerViewModel row })
            {
                vm.SetLayerHidden(row.Layer, row.IsVisible);
            }
        }

        private void LayerDelete_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is MenuItem { Tag: LayerViewModel row })
            {
                vm.DeleteLayer(row.Layer);
            }
        }

        private void ZoomIn_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1.2);
        }

        private void ZoomOut_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.ZoomBy(1 / 1.2);
        }

        private void ZoomFit_Click(object? sender, RoutedEventArgs e)
        {
            this.FindControl<LevelCanvas>("Canvas")!.FitToView();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
                row.IsRenaming = false;
                _ = _canvas.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CommitLayerRename(row, row.Name);
                _ = _canvas.Focus();
                e.Handled = true;
            }
        }

        private void LayerName_Commit(object? sender, RoutedEventArgs e)
        {
            if (sender is TextBox { Tag: LayerViewModel row, Text: { } name })
            {
                CommitLayerRename(row, name);
            }
        }

        private void CommitLayerRename(LayerViewModel row, string name)
        {
            if (DataContext is EditorViewModel vm
                && name != row.Layer.Name
                && !vm.RenameLayer(row.Layer, name))
            {
                row.Name = row.Layer.Name;
            }
            else if (DataContext is not EditorViewModel)
            {
                row.Name = row.Layer.Name;
            }

            row.IsRenaming = false;
        }

        private void LayerName_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Control { Tag: LayerViewModel row })
            {
                BeginLayerRename(row);
                e.Handled = true;
            }
        }

        private void LayerTree_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2
                && DataContext is EditorViewModel { SelectedTreeItem: LayerViewModel row })
            {
                BeginLayerRename(row);
                e.Handled = true;
            }
        }

        private void BeginLayerRename(LayerViewModel row)
        {
            row.Name = row.Layer.Name;
            row.IsRenaming = true;
            Dispatcher.UIThread.Post(() =>
            {
                TextBox? editor = this.FindControl<TreeView>("LayersTree")?
                    .GetVisualDescendants()
                    .OfType<TextBox>()
                    .FirstOrDefault(candidate =>
                        candidate.Classes.Contains("layer-name-editor")
                        && ReferenceEquals(candidate.Tag, row));
                if (editor is not null)
                {
                    _ = editor.Focus();
                    editor.SelectAll();
                }
            }, DispatcherPriority.Loaded);
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
                _ = vm.MoveObjectToLayer(target.Object, target.Layer);
            }
        }

        private sealed record MoveTarget(LevelObject Object, LevelLayer Layer);

        private static readonly DataFormat<LayerViewModel> LayerDragFormat =
            DataFormat.CreateInProcessFormat<LayerViewModel>("ctrdx-layer-row");
        private static readonly DataFormat<LevelObject> ObjectDragFormat =
            DataFormat.CreateInProcessFormat<LevelObject>("ctrdx-object-row");
        private LayerViewModel? _dragLayer;
        private PointerPressedEventArgs? _dragTrigger;
        private Point _dragStart;
        private LevelObject? _dragObject;
        private PointerPressedEventArgs? _objectDragTrigger;
        private Point _objectDragStart;
        private Border? _layerDropTarget;

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

        private void ObjectRow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: LevelObject obj }
                && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                _dragObject = obj;
                _objectDragTrigger = e;
                _objectDragStart = e.GetPosition(null);
            }
        }

        private async void ObjectRow_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                ClearPendingObjectDrag();
                return;
            }

            if (_dragObject is not { } obj || _objectDragTrigger is not { } trigger)
            {
                return;
            }

            Point now = e.GetPosition(null);
            if (Math.Abs(now.X - _objectDragStart.X) < 4 && Math.Abs(now.Y - _objectDragStart.Y) < 4)
            {
                return;
            }

            ClearPendingObjectDrag();
            DataTransfer data = new();
            data.Add(DataTransferItem.Create(ObjectDragFormat, obj));
            _ = await DragDrop.DoDragDropAsync(trigger, data, DragDropEffects.Move);
            ClearLayerDropTarget();
        }

        private void ObjectRow_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            ClearPendingObjectDrag();
        }

        private void ObjectRow_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            ClearPendingObjectDrag();
        }

        private void ClearPendingObjectDrag()
        {
            _dragObject = null;
            _objectDragTrigger = null;
        }

        private void LayerRow_DragOver(object? sender, DragEventArgs e)
        {
            bool acceptsObject = e.DataTransfer.Contains(ObjectDragFormat);
            e.DragEffects = acceptsObject || e.DataTransfer.Contains(LayerDragFormat)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            if (acceptsObject)
            {
                SetLayerDropTarget(sender as Border);
            }
            else if (ReferenceEquals(sender, _layerDropTarget))
            {
                ClearLayerDropTarget();
            }
        }

        private void LayerRow_DragLeave(object? sender, DragEventArgs e)
        {
            if (ReferenceEquals(sender, _layerDropTarget))
            {
                ClearLayerDropTarget();
            }
        }

        private void LayerRow_Drop(object? sender, DragEventArgs e)
        {
            ClearLayerDropTarget();
            if (DataContext is not EditorViewModel vm
                || sender is not Control { DataContext: LayerViewModel target })
            {
                return;
            }

            if (e.DataTransfer.TryGetValue(ObjectDragFormat) is LevelObject obj)
            {
                e.DragEffects = vm.MoveObjectToLayer(obj, target.Layer)
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (e.DataTransfer.TryGetValue(LayerDragFormat) is not LayerViewModel source
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

        private void SetLayerDropTarget(Border? target)
        {
            if (ReferenceEquals(target, _layerDropTarget))
            {
                return;
            }

            ClearLayerDropTarget();
            _layerDropTarget = target;
            _layerDropTarget?.Classes.Add("drop-target");
        }

        private void ClearLayerDropTarget()
        {
            if (_layerDropTarget is not null)
            {
                _ = _layerDropTarget.Classes.Remove("drop-target");
                _layerDropTarget = null;
            }
        }

        private void LayerContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            if (sender is not ContextMenu { Tag: LayerViewModel row } menu)
            {
                e.Cancel = true;
                return;
            }

            MenuItem rename = new() { Header = Localizer.Get("Layer.MenuRename"), Tag = row };
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
            if (sender is Control { Tag: LayerViewModel row })
            {
                BeginLayerRename(row);
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

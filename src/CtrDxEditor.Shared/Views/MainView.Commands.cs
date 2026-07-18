using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
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
                TextBox? editor = FindLayerNameEditor(row);
                if (editor is not null)
                {
                    _ = editor.Focus();
                    editor.SelectAll();
                }
            }, DispatcherPriority.Normal);
        }

        private TextBox? FindLayerNameEditor(LayerViewModel row)
        {
            return this.FindControl<TreeView>("LayersTree")?
                .GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(candidate =>
                    candidate.Classes.Contains("layer-name-editor")
                    && ReferenceEquals(candidate.Tag, row));
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

        private LayerViewModel? _dragLayer;
        private Point _dragStart;
        private LevelObject? _dragObject;
        private Point _objectDragStart;
        private Border? _layerDropTarget;
        private IPointer? _rowDragPointer;
        private bool _rowDragActive;

        private void LayerRow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (IsLayerRowAction(e.Source))
            {
                ClearPendingLayerDrag();
                return;
            }

            if (sender is Control { DataContext: LayerViewModel row }
                && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                CancelRowDrag();
                _dragLayer = row;
                _dragStart = e.GetPosition(null);
            }
        }

        private static bool IsLayerRowAction(object? source)
        {
            for (Visual? current = source as Visual; current is not null; current = current.GetVisualParent())
            {
                if (current is Border border && border.Classes.Contains("layer-row"))
                {
                    return false;
                }

                if (current is Button or ToggleButton or TextBox)
                {
                    return true;
                }
            }

            return false;
        }

        private void LayerRow_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                CancelRowDrag();
                return;
            }

            if (_dragLayer is null)
            {
                return;
            }

            if (!_rowDragActive)
            {
                Point now = e.GetPosition(null);
                if (Math.Abs(now.X - _dragStart.X) < 4 && Math.Abs(now.Y - _dragStart.Y) < 4)
                {
                    return;
                }

                BeginRowDrag(sender as Visual, e.Pointer);
            }

            UpdateRowDrag(e);
        }

        private void LayerRow_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            CompleteRowDrag();
        }

        private void LayerRow_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            CancelRowDrag();
        }

        private void ClearPendingLayerDrag()
        {
            _dragLayer = null;
        }

        private void ObjectRow_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Control { DataContext: LevelObject obj }
                && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                CancelRowDrag();
                _dragObject = obj;
                _objectDragStart = e.GetPosition(null);
            }
        }

        private void ObjectRow_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                CancelRowDrag();
                return;
            }

            if (_dragObject is null)
            {
                return;
            }

            if (!_rowDragActive)
            {
                Point now = e.GetPosition(null);
                if (Math.Abs(now.X - _objectDragStart.X) < 4 && Math.Abs(now.Y - _objectDragStart.Y) < 4)
                {
                    return;
                }

                BeginRowDrag(sender as Visual, e.Pointer);
            }

            UpdateRowDrag(e);
        }

        private void ObjectRow_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            CompleteRowDrag();
        }

        private void ObjectRow_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            CancelRowDrag();
        }

        private void ClearPendingObjectDrag()
        {
            _dragObject = null;
        }

        private void BeginRowDrag(Visual? row, IPointer pointer)
        {
            if (row is null)
            {
                return;
            }

            _rowDragActive = true;
            _rowDragPointer = pointer;
            double scale = TopLevel.GetTopLevel(row)?.RenderScaling ?? 1.0;
            PixelSize pixelSize = PixelSize.FromSize(row.Bounds.Size, scale);
            if (pixelSize.Width > 0 && pixelSize.Height > 0)
            {
                RenderTargetBitmap preview = new(
                    pixelSize,
                    new Vector(96 * scale, 96 * scale));
                preview.Render(row);
                _rowDragPreview.Source = preview;
                _rowDragPreview.Width = row.Bounds.Width;
                _rowDragPreview.Height = row.Bounds.Height;
                _rowDragPreview.IsVisible = true;
            }

            pointer.Capture(row as IInputElement);
        }

        private void UpdateRowDrag(PointerEventArgs e)
        {
            if (!_rowDragActive || this.FindControl<TreeView>("LayersTree") is not { } layersTree)
            {
                return;
            }

            Point previewPosition = e.GetPosition(this);
            Avalonia.Controls.Canvas.SetLeft(_rowDragPreview, previewPosition.X + 12);
            Avalonia.Controls.Canvas.SetTop(_rowDragPreview, previewPosition.Y + 12);
            UpdateLayerDropTarget(e.GetPosition(layersTree));
        }

        private void UpdateLayerDropTarget(Point position)
        {
            TreeView? layersTree = this.FindControl<TreeView>("LayersTree");
            Visual? hit = layersTree?.InputHitTest(position) as Visual;
            SetLayerDropTarget(FindLayerDropTarget(hit));
        }

        private static Border? FindLayerDropTarget(Visual? hit)
        {
            for (Visual? current = hit; current is not null; current = current.GetVisualParent())
            {
                if (current is Border border
                    && border.Classes.Contains("layer-row")
                    && border.DataContext is LayerViewModel)
                {
                    return border;
                }

                if (current is TreeViewItem { DataContext: LayerViewModel } layerItem)
                {
                    return layerItem.GetVisualDescendants()
                        .OfType<Border>()
                        .FirstOrDefault(candidate => candidate.Classes.Contains("layer-row"));
                }
            }

            return null;
        }

        private void CompleteRowDrag()
        {
            bool shouldMove = _rowDragActive;
            LayerViewModel? sourceLayer = _dragLayer;
            LevelObject? sourceObject = _dragObject;
            LayerViewModel? targetLayer = _layerDropTarget?.DataContext is LayerViewModel target
                ? target
                : null;
            CancelRowDrag();

            if (!shouldMove || targetLayer is null || DataContext is not EditorViewModel vm)
            {
                return;
            }

            if (sourceObject is not null)
            {
                _ = vm.MoveObjectToLayer(sourceObject, targetLayer.Layer);
                return;
            }

            if (sourceLayer is not null && !ReferenceEquals(sourceLayer, targetLayer))
            {
                int targetIndex = vm.Layers.IndexOf(targetLayer);
                if (targetIndex >= 0)
                {
                    vm.MoveLayerToIndex(sourceLayer.Layer, targetIndex);
                }
            }
        }

        private void CancelRowDrag()
        {
            IPointer? pointer = _rowDragPointer;
            _rowDragActive = false;
            _rowDragPointer = null;
            ClearPendingLayerDrag();
            ClearPendingObjectDrag();
            ClearLayerDropTarget();
            _rowDragPreview.IsVisible = false;
            IDisposable? preview = _rowDragPreview.Source as IDisposable;
            _rowDragPreview.Source = null;
            preview?.Dispose();
            pointer?.Capture(null);
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
                if (row.IsRenaming)
                {
                    TextBox? editor = FindLayerNameEditor(row);
                    CommitLayerRename(row, editor?.Text ?? row.Name);
                    _ = _canvas.Focus();
                }
                else
                {
                    BeginLayerRename(row);
                }
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

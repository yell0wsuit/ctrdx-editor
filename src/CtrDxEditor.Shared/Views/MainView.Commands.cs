using System;
using System.Linq;
using System.Threading.Tasks;

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
                vm.ToggleObjectVisibility(obj);
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

        private LayerViewModel? _dragLayer;
        private Point _dragStart;
        private LevelObject? _dragObject;
        private Point _objectDragStart;
        private Border? _layerDropTarget;
        private IPointer? _rowDragPointer;
        private Point _rowDragGrabOffset;
        private bool _rowDragActive;
        private Point _rowDragTreePosition;
        private DispatcherTimer? _rowDragScrollTimer;

        // Blocks the platform command modifier (Cmd on macOS, Ctrl elsewhere) from reaching the tree's
        // single-selection logic, which would otherwise toggle the clicked row off.
        private void LayersTree_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(CmdModifier))
            {
                e.Handled = true;
            }
        }

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

                BeginRowDrag(sender as Visual, e);
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

                BeginRowDrag(sender as Visual, e);
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

        private void BeginRowDrag(Visual? row, PointerEventArgs e)
        {
            if (row is null)
            {
                return;
            }

            _rowDragActive = true;
            _rowDragPointer = e.Pointer;
            _rowDragGrabOffset = e.GetPosition(row);
            // RenderTargetBitmap.Render draws the visual at 1 DIP = 1 pixel (it ignores the bitmap
            // DPI), so the pixel buffer must match the row's DIP size. Oversizing it by the render
            // scale left the row rendered into a corner, and Stretch=Fill then magnified that
            // fragment ("enlarged / half cut off").
            PixelSize pixelSize = PixelSize.FromSize(row.Bounds.Size, 1.0);
            if (pixelSize.Width > 0 && pixelSize.Height > 0)
            {
                RenderTargetBitmap preview = new(pixelSize, new Vector(96, 96));
                preview.Render(row);
                _rowDragPreview.Source = preview;
                _rowDragPreview.Width = row.Bounds.Width;
                _rowDragPreview.Height = row.Bounds.Height;
                _rowDragPreview.IsVisible = true;
            }

            e.Pointer.Capture(row as IInputElement);
        }

        private void UpdateRowDrag(PointerEventArgs e)
        {
            if (!_rowDragActive || this.FindControl<TreeView>("LayersTree") is not { } layersTree)
            {
                return;
            }

            Point previewPosition = e.GetPosition(this);
            Point alignedPosition = GetRowDragPreviewPosition(previewPosition, _rowDragGrabOffset);
            Avalonia.Controls.Canvas.SetLeft(_rowDragPreview, alignedPosition.X);
            Avalonia.Controls.Canvas.SetTop(_rowDragPreview, alignedPosition.Y);

            _rowDragTreePosition = e.GetPosition(layersTree);
            EnsureRowDragScrollTimer();
            UpdateLayerDropTarget(_rowDragTreePosition);
        }

        // Auto-scroll the layer tree while a row drag hovers near its top/bottom edge, so a drop
        // target that is off-screen can still be reached. A timer drives it (rather than only the
        // pointer-moved events) so scrolling continues while the cursor is held still at the edge.
        private void EnsureRowDragScrollTimer()
        {
            _rowDragScrollTimer ??= new DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                DispatcherPriority.Background,
                RowDragScrollTick);
            _rowDragScrollTimer.Start();
        }

        private void RowDragScrollTick(object? sender, EventArgs e)
        {
            if (!_rowDragActive
                || this.FindControl<TreeView>("LayersTree") is not { } layersTree
                || layersTree.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault() is not { } scrollViewer)
            {
                return;
            }

            const double edgeBand = 28;   // activation zone at each edge
            const double maxStep = 14;     // px scrolled per tick at the very edge
            double height = layersTree.Bounds.Height;
            double y = _rowDragTreePosition.Y;

            double delta = 0;
            if (y < edgeBand)
            {
                delta = -maxStep * Math.Min(1.0, (edgeBand - y) / edgeBand);
            }
            else if (y > height - edgeBand)
            {
                delta = maxStep * Math.Min(1.0, (y - (height - edgeBand)) / edgeBand);
            }

            if (delta == 0)
            {
                return;
            }

            double maxOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            double newY = Math.Clamp(scrollViewer.Offset.Y + delta, 0, maxOffset);
            if (Math.Abs(newY - scrollViewer.Offset.Y) > double.Epsilon)
            {
                scrollViewer.Offset = scrollViewer.Offset.WithY(newY);
                UpdateLayerDropTarget(_rowDragTreePosition);
            }
        }

        private static Point GetRowDragPreviewPosition(Point pointerPosition, Point grabOffset)
        {
            return new Point(
                pointerPosition.X - grabOffset.X,
                pointerPosition.Y - grabOffset.Y);
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
            _rowDragGrabOffset = default;
            _rowDragScrollTimer?.Stop();
            _rowDragTreePosition = default;
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

        private void LayerLockToggle_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && sender is Control { Tag: LayerViewModel row })
            {
                vm.SetLayerLocked(row.Layer, !vm.IsLayerLocked(row.Layer));
            }
        }

        private async void LayerDeleteActive_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel { ActiveLayer: { } active } vm
                && await ConfirmDeleteLayerAsync(active.Layer.Name))
            {
                vm.DeleteActiveLayer();
            }
        }

        // Confirms destructive layer removal (the layer plus every object in it) before it happens.
        private static async Task<bool> ConfirmDeleteLayerAsync(string layerName)
        {
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.DeleteLayer.Header"),
                Message = Localizer.Get("Dialog.DeleteLayer.Body").Replace("{0}", layerName, StringComparison.Ordinal),
                PositiveText = Localizer.Get("Dialog.DeleteLayer.Confirm"),
                NegativeText = Localizer.Get("Dialog.Common.Cancel"),
            };
            return (await dialog.ShowAsync()).GetValueOrDefault();
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

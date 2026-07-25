using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Main editor view containing menus, palette, property panel, object list, and canvas.</summary>
    public partial class MainView : UserControl
    {
        private EditorViewModel? _mutatedSubscription;
        private EditorSelection? _selectionSubscription;
        // Guards the two-way object-panel <-> selection sync from looping.
        private bool _syncingTreeSelection;
        private readonly Action _invalidateCanvas;
        private readonly PaletteDragController _paletteDrag;
        private WindowNotificationManager? _notifications;
        private readonly LevelCanvas _canvas;
        private readonly TextBox _textEditor;
        private readonly Image _rowDragPreview;
        private LevelObject? _editingText;

        /// <summary>Creates the main editor view and wires input gestures.</summary>
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            _canvas = canvas;
            _textEditor = this.FindControl<TextBox>("TextEditor")!;
            _rowDragPreview = this.FindControl<Image>("RowDragPreview")!;
            _invalidateCanvas = canvas.InvalidateVisual;
            DataContextChanged += (_, _) => WireObjectMutated();
            WireObjectMutated();
            _animationPreviewTimer.Tick += AnimationPreviewTimer_Tick;

            canvas.PlaceAt = (element, x, y) =>
                DataContext is EditorViewModel vm ? vm.PlaceObject(element, x, y) : null;
            canvas.ToggleLock = obj => (DataContext as EditorViewModel)?.ToggleLock(obj);
            canvas.SelectionRequested = request =>
            {
                if (DataContext is not EditorViewModel vm)
                {
                    return;
                }

                switch (request.Kind)
                {
                    case LevelCanvas.SelectionRequestKind.Replace:
                        if (request.Target is { } replaceTarget)
                        {
                            vm.Selection.Replace(replaceTarget);
                        }
                        break;
                    case LevelCanvas.SelectionRequestKind.Toggle:
                        if (request.Target is { } toggleTarget)
                        {
                            vm.Selection.Toggle(toggleTarget);
                        }
                        break;
                    case LevelCanvas.SelectionRequestKind.Clear:
                        if (!vm.CollapseLayerSelectionToActive())
                        {
                            vm.Selection.Clear();
                        }
                        break;
                    default:
                        break;
                }
                vm.RaiseSelectedObjectChanged();
            };
            canvas.SelectedObjectMoved = () => (DataContext as EditorViewModel)?.RefreshFieldValues();
            canvas.DuplicateRequested = () => (DataContext as EditorViewModel)?.DuplicateSelection(0, 0);
            canvas.HandSegmentActivated = index => (DataContext as EditorViewModel)?.ExpandFieldGroup(index);
            canvas.BeginDocumentEdit = () => (DataContext as EditorViewModel)?.BeginUndoTransaction();
            canvas.CompleteDocumentEdit = () => (DataContext as EditorViewModel)?.CompleteUndoTransaction();
            canvas.EditTutorialTextRequested = BeginTextEdit;
            canvas.PressIntercepted = DismissCompactDrawerOnCanvasPress;
            EmptyStateView emptyState = this.FindControl<EmptyStateView>("EmptyState")!;
            emptyState.NewRequested = () => New_Click(this, new RoutedEventArgs());
            emptyState.OpenRequested = () => Open_Click(this, new RoutedEventArgs());
            emptyState.UsageGuideRequested = () => UsageGuide_Click(this, new RoutedEventArgs());

            // Palette placement is an internal pointer-capture drag (see PaletteDragController). Buttons mark
            // their own left PointerPressed as Handled for click logic, so the handlers are registered with
            // handledEventsToo to still see it.
            _paletteDrag = new PaletteDragController(this, canvas);
            ItemsControl paletteList = this.FindControl<PaletteView>("Palette")!.ItemsHost;
            paletteList.AddHandler(
                PointerPressedEvent, _paletteDrag.OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerMovedEvent, _paletteDrag.OnPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerReleasedEvent, _paletteDrag.OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerCaptureLostEvent, _paletteDrag.OnPointerCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
            // Observe every release in the view so a captured row drag also completes when released outside
            // its source row. Before the movement threshold, row controls retain normal click/edit behavior.
            AddHandler(PointerReleasedEvent, LayerRow_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(PointerReleasedEvent, ObjectRow_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            // Delete and Space are focus-gated on the bubble path; the Cmd/Ctrl menu chords are handled
            // globally at the TopLevel (see MainView.Shortcuts.cs). Menu hint text is bound in XAML via
            // ShortcutHint.
            WireLocalShortcuts();
            WireLayoutMode();
        }

        private void WireObjectMutated()
        {
            if (ReferenceEquals(_mutatedSubscription, DataContext))
            {
                return;
            }

            _mutatedSubscription?.ObjectMutated -= _invalidateCanvas;
            _mutatedSubscription?.LevelLoaded -= FocusCanvasAfterLevelLoaded;
            _mutatedSubscription?.PropertyChanged -= ViewModel_PropertyChanged;
            _selectionSubscription?.Changed -= SyncCanvasSelection;
            _selectionSubscription = null;

            _mutatedSubscription = DataContext as EditorViewModel;
            if (_mutatedSubscription is not null)
            {
                _mutatedSubscription.ObjectMutated += _invalidateCanvas;
                _mutatedSubscription.LevelLoaded += FocusCanvasAfterLevelLoaded;
                _mutatedSubscription.PropertyChanged += ViewModel_PropertyChanged;
                SubscribeToSelection(_mutatedSubscription.Selection);
            }

            SyncAnimationPreviewTimer();
            // A swapped view model carries its own document state, which the compact chrome is gated on.
            UpdateCompactChromeVisibility();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditorViewModel.AnimationPreviewMode) or nameof(EditorViewModel.AnimationPreviewObject))
            {
                SyncAnimationPreviewTimer();
            }

            if (e.PropertyName == nameof(EditorViewModel.SelectedTreeItem))
            {
                Dispatcher.UIThread.Post(BringSelectedTreeItemIntoView, DispatcherPriority.Loaded);
            }

            if (e.PropertyName == nameof(EditorViewModel.HasDocument))
            {
                UpdateCompactChromeVisibility();
            }

            if (e.PropertyName is nameof(EditorViewModel.CanCutSelection)
                or nameof(EditorViewModel.CanPaste)
                or nameof(EditorViewModel.SelectedObject))
            {
                UpdateCompactEditBarVisibility();
            }

            if (e.PropertyName == nameof(EditorViewModel.EffectivelyLockedObjects))
            {
                ClearLockedTreeSelection();
            }

            if (e.PropertyName == nameof(EditorViewModel.Selection)
                && sender is EditorViewModel vm)
            {
                SubscribeToSelection(vm.Selection);
            }

            if (e.PropertyName == nameof(EditorViewModel.SelectedLayers))
            {
                SyncTreeFromModel();
            }
        }

        private void SubscribeToSelection(EditorSelection selection)
        {
            if (ReferenceEquals(_selectionSubscription, selection))
            {
                return;
            }

            _selectionSubscription?.Changed -= SyncCanvasSelection;
            _selectionSubscription = selection;
            _selectionSubscription.Changed += SyncCanvasSelection;
            SyncCanvasSelection();
        }

        private void SyncCanvasSelection()
        {
            if (_selectionSubscription is null)
            {
                return;
            }

            _canvas.SelectedObjects = new HashSet<LevelObject>(_selectionSubscription.Items);
            _canvas.PrimaryObject = _selectionSubscription.Primary;
            _canvas.InvalidateVisual();
            SyncTreeFromModel();
        }

        // Mirrors the active model selection mode into the tree's SelectedItems. Guarded so the resulting
        // SelectionChanged does not translate straight back into the model.
        private void SyncTreeFromModel()
        {
            if (_mutatedSubscription is not { } vm
                || this.FindControl<TreeView>("LayersTree") is not { } tree)
            {
                return;
            }

            object[] target = vm.SelectedLayers.Count > 0
                ? [.. vm.SelectedLayers.Cast<object>()]
                : [.. (_selectionSubscription?.Items ?? []).Cast<object>()];
            object[] current = [.. tree.SelectedItems.Cast<object>()];
            if (target.Length == current.Length && !target.Except(current).Any())
            {
                return;
            }

            _syncingTreeSelection = true;
            try
            {
                tree.SelectedItems.Clear();
                foreach (object item in target)
                {
                    _ = tree.SelectedItems.Add(item);
                }
            }
            finally
            {
                _syncingTreeSelection = false;
            }
        }

        // Blank tree space collapses a multi-layer selection without disturbing row or scrollbar input.
        private void LayersTree_PointerPressed(object? _, PointerPressedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm
                || e.GetCurrentPoint(null).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed
                || e.Source is not Visual source
                || source.GetVisualAncestors().Prepend(source)
                    .Any(visual => visual is TreeViewItem or ScrollBar))
            {
                return;
            }

            if (vm.CollapseLayerSelectionToActive())
            {
                e.Handled = true;
            }
        }

        // Translates the object panel's selected rows into the mutually exclusive model selection modes.
        private void LayersTree_SelectionChanged(object? _, SelectionChangedEventArgs e)
        {
            ClearLockedTreeSelection();

            if (_syncingTreeSelection
                || DataContext is not EditorViewModel vm
                || this.FindControl<TreeView>("LayersTree") is not { } tree)
            {
                return;
            }

            List<LevelObject> objects = [.. tree.SelectedItems.OfType<LevelObject>()];
            List<LayerViewModel> layers = [.. tree.SelectedItems.OfType<LayerViewModel>()];
            // The just-touched kind wins so the two stay mutually exclusive.
            bool addedLayer = e.AddedItems.OfType<LayerViewModel>().Any();

            _syncingTreeSelection = true;
            try
            {
                if (addedLayer && layers.Count > 0)
                {
                    vm.SetLayerSelection(layers, layers[^1]);
                    RemoveFromTreeSelection(tree, tree.SelectedItems.OfType<LevelObject>().Cast<object>());
                }
                else if (objects.Count > 0)
                {
                    vm.SetObjectSelection(objects);
                    RemoveFromTreeSelection(tree, tree.SelectedItems.OfType<LayerViewModel>().Cast<object>());
                }
                else if (layers.Count > 0)
                {
                    vm.SetLayerSelection(layers, layers[^1]);
                }
                else
                {
                    vm.ClearLayerSelection();
                    vm.Selection.Clear();
                    vm.RaiseSelectedObjectChanged();
                }
            }
            finally
            {
                _syncingTreeSelection = false;
            }
        }

        private static void RemoveFromTreeSelection(TreeView tree, IEnumerable<object> items)
        {
            foreach (object item in items.ToList())
            {
                tree.SelectedItems.Remove(item);
            }
        }

        private void ClearLockedTreeSelection()
        {
            if (DataContext is not EditorViewModel vm
                || this.FindControl<TreeView>("LayersTree") is not { } tree
                || tree.SelectedItem is not LevelObject selected
                || !vm.EffectivelyLockedObjects.Contains(selected))
            {
                return;
            }

            tree.SelectedItem = null;
            vm.SelectedTreeItem = null;
        }

        private void BringSelectedTreeItemIntoView()
        {
            if (DataContext is not EditorViewModel { SelectedTreeItem: { } selected }
                || this.FindControl<TreeView>("LayersTree") is not { } tree
                || tree.GetVisualDescendants().OfType<TreeViewItem>()
                    .FirstOrDefault(item => ReferenceEquals(item.DataContext, selected)) is not { } container)
            {
                return;
            }

            container.BringIntoView();
        }

        private void FocusCanvasAfterLevelLoaded()
        {
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            canvas.FitToView();
            _ = canvas.Focus();
        }

        /// <summary>Opens the inline text editor over a tutorial text from the canvas F2 shortcut.</summary>
        private void BeginTextEdit(LevelObject obj)
        {
            _editingText = obj;
            Rect r = _canvas.TutorialTextScreenRect(obj);
            _textEditor.Margin = new Thickness(Math.Max(0, r.X), Math.Max(0, r.Y), 0, 0);
            _textEditor.Width = Math.Max(80, r.Width);
            _textEditor.Height = Math.Max(28, r.Height + 6);
            _textEditor.Text = obj.GetAttr("text") ?? string.Empty;
            _textEditor.IsVisible = true;
            _ = _textEditor.Focus();
            _textEditor.SelectAll();
        }

        private void TextEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                CommitTextEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                EndTextEdit();
                e.Handled = true;
            }
        }

        private void TextEditor_LostFocus(object? sender, RoutedEventArgs e)
        {
            CommitTextEdit();
        }

        private void CommitTextEdit()
        {
            if (_editingText is { } obj && DataContext is EditorViewModel vm)
            {
                vm.CommitTutorialText(obj, _textEditor.Text ?? string.Empty);
            }
            EndTextEdit();
        }

        private void EndTextEdit()
        {
            if (_editingText is null)
            {
                return;
            }
            _editingText = null;
            _textEditor.IsVisible = false;
            _ = _canvas.Focus();
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Create the toast host now, while there is a TopLevel, so it is attached and laid out before
            // the first save. A manager constructed at the first Show() call drops that first notification
            // (it is not yet in the visual tree), which is why the initial "Saving…" toast went missing.
            _ = Notifications();
            RegisterGlobalShortcuts();
            RegisterLevelDrop();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            UnregisterGlobalShortcuts();
            UnregisterLevelDrop();
            _paletteDrag.Cancel();
            _animationPreviewTimer.Stop();
            if (_mutatedSubscription is not null)
            {
                _mutatedSubscription.ObjectMutated -= _invalidateCanvas;
                _mutatedSubscription.LevelLoaded -= FocusCanvasAfterLevelLoaded;
                _mutatedSubscription.PropertyChanged -= ViewModel_PropertyChanged;
                _mutatedSubscription = null;
            }
            base.OnDetachedFromVisualTree(e);
        }

        // The toast host, created against the current TopLevel and reused. Null only before the view is
        // attached to a window, which cannot happen from a menu click.
        private WindowNotificationManager? Notifications()
        {
            if (_notifications is null && TopLevel.GetTopLevel(this) is { } top)
            {
                _notifications = new WindowNotificationManager(top)
                {
                    Position = NotificationPosition.BottomRight,
                    // One at a time: showing the terminal toast evicts the sticky "Saving…" one, so the
                    // screenshot save reads as a single toast that updates in place.
                    MaxItems = 1,
                };
            }
            return _notifications;
        }

    }
}

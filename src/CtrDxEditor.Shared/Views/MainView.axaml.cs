using System;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Main editor view containing menus, palette, property panel, object list, and canvas.</summary>
    public partial class MainView : UserControl
    {
        private EditorViewModel? _mutatedSubscription;
        private readonly Action _invalidateCanvas;
        private readonly PaletteDragController _paletteDrag;
        private WindowNotificationManager? _notifications;

        /// <summary>Creates the main editor view and wires input gestures.</summary>
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            _invalidateCanvas = canvas.InvalidateVisual;
            DataContextChanged += (_, _) => WireObjectMutated();
            WireObjectMutated();
            _animationPreviewTimer.Tick += AnimationPreviewTimer_Tick;

            canvas.PlaceAt = (element, x, y) =>
                DataContext is EditorViewModel vm ? vm.PlaceObject(element, x, y) : null;
            canvas.ToggleLock = obj => (DataContext as EditorViewModel)?.ToggleLock(obj);
            canvas.SelectedObjectMoved = () => (DataContext as EditorViewModel)?.RefreshFieldValues();
            canvas.BeginDocumentEdit = () => (DataContext as EditorViewModel)?.BeginUndoTransaction();
            canvas.CompleteDocumentEdit = () => (DataContext as EditorViewModel)?.CompleteUndoTransaction();

            // Palette placement is an internal pointer-capture drag (see PaletteDragController). Buttons mark
            // their own left PointerPressed as Handled for click logic, so the handlers are registered with
            // handledEventsToo to still see it.
            _paletteDrag = new PaletteDragController(this, canvas);
            ItemsControl paletteList = this.FindControl<ItemsControl>("PaletteList")!;
            paletteList.AddHandler(
                PointerPressedEvent, _paletteDrag.OnPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerMovedEvent, _paletteDrag.OnPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerReleasedEvent, _paletteDrag.OnPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            paletteList.AddHandler(
                PointerCaptureLostEvent, _paletteDrag.OnPointerCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
            // Delete and Space are focus-gated on the bubble path; the Cmd/Ctrl menu chords are handled
            // globally at the TopLevel (see MainView.Shortcuts.cs). Menu hint text is bound in XAML via
            // ShortcutHint.
            WireLocalShortcuts();
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

            _mutatedSubscription = DataContext as EditorViewModel;
            if (_mutatedSubscription is not null)
            {
                _mutatedSubscription.ObjectMutated += _invalidateCanvas;
                _mutatedSubscription.LevelLoaded += FocusCanvasAfterLevelLoaded;
                _mutatedSubscription.PropertyChanged += ViewModel_PropertyChanged;
            }

            SyncAnimationPreviewTimer();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditorViewModel.AnimationPreviewMode) or nameof(EditorViewModel.AnimationPreviewObject))
            {
                SyncAnimationPreviewTimer();
            }
        }

        private void FocusCanvasAfterLevelLoaded()
        {
            LevelCanvas canvas = this.FindControl<LevelCanvas>("Canvas")!;
            canvas.FitToView();
            _ = canvas.Focus();
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
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            UnregisterGlobalShortcuts();
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Interactive editor canvas for rendering, selecting, dragging, zooming, and placing level objects.</summary>
    public sealed partial class LevelCanvas : Control
    {
        /// <summary>Avalonia property backing <see cref="Document"/>.</summary>
        public static readonly StyledProperty<LevelDocument?> DocumentProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelDocument?>(nameof(Document));

        /// <summary>Avalonia property backing <see cref="Sprites"/>.</summary>
        public static readonly StyledProperty<SpriteCache?> SpritesProperty =
            AvaloniaProperty.Register<LevelCanvas, SpriteCache?>(nameof(Sprites));

        /// <summary>Avalonia property backing <see cref="View"/>.</summary>
        public static readonly StyledProperty<ViewTransform> ViewProperty =
            AvaloniaProperty.Register<LevelCanvas, ViewTransform>(nameof(View), ViewTransform.Identity);

        /// <summary>Avalonia property backing <see cref="SnapEnabled"/>.</summary>
        public static readonly StyledProperty<bool> SnapEnabledProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(SnapEnabled));

        /// <summary>Avalonia property backing <see cref="SelectedObject"/>.</summary>
        public static readonly StyledProperty<LevelObject?> SelectedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(SelectedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="LockedObject"/>.</summary>
        public static readonly StyledProperty<LevelObject?> LockedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(LockedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="ShowHitboxes"/>.</summary>
        public static readonly StyledProperty<bool> ShowHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowHitboxes), defaultValue: true);

        /// <summary>Avalonia property backing <see cref="ShowMobileHitboxes"/>.</summary>
        public static readonly StyledProperty<bool> ShowMobileHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowMobileHitboxes));

        /// <summary>Editor-decoration rope skin index applied to every rope (0 = default brown).</summary>
        public static readonly StyledProperty<int> ActiveRopeSkinProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveRopeSkin));

        /// <summary>Editor-decoration background id (0 = none, 1..7 = bgr_01..bgr_07).</summary>
        public static readonly StyledProperty<int> ActiveBackgroundProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveBackground));

        /// <summary>Editor-decoration candy skin index applied to candy sprites (0 = default candy).</summary>
        public static readonly StyledProperty<int> ActiveCandySkinProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveCandySkin));

        /// <summary>Editor-decoration Om Nom sitting platform index applied to the target (0 = default).</summary>
        public static readonly StyledProperty<int> ActiveOmNomSupportProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveOmNomSupport));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollMaximum"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollMaximumProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(HorizontalScrollMaximum));

        /// <summary>Avalonia property backing <see cref="VerticalScrollMaximum"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollMaximumProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(VerticalScrollMaximum));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollViewport"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollViewportProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(HorizontalScrollViewport));

        /// <summary>Avalonia property backing <see cref="VerticalScrollViewport"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollViewportProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(VerticalScrollViewport));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollValue"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollValueProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(
                nameof(HorizontalScrollValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="VerticalScrollValue"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollValueProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(
                nameof(VerticalScrollValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        static LevelCanvas()
        {
            AffectsRender<LevelCanvas>(
                DocumentProperty, SpritesProperty, ViewProperty, SnapEnabledProperty,
                SelectedObjectProperty, LockedObjectProperty,
                ShowHitboxesProperty, ShowMobileHitboxesProperty,
                ActiveRopeSkinProperty, ActiveBackgroundProperty, ActiveCandySkinProperty,
                ActiveOmNomSupportProperty);
        }

        /// <summary>The loaded level document to render and edit.</summary>
        public LevelDocument? Document { get => GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

        /// <summary>Sprite cache used to render object art.</summary>
        public SpriteCache? Sprites { get => GetValue(SpritesProperty); set => SetValue(SpritesProperty, value); }

        /// <summary>Current zoom and pan transform.</summary>
        public ViewTransform View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }

        /// <summary>Whether object moves and placements snap to the level grid.</summary>
        public bool SnapEnabled { get => GetValue(SnapEnabledProperty); set => SetValue(SnapEnabledProperty, value); }

        /// <summary>The currently selected object, if any.</summary>
        public LevelObject? SelectedObject { get => GetValue(SelectedObjectProperty); set => SetValue(SelectedObjectProperty, value); }

        /// <summary>The object locked for exclusive interaction, if any.</summary>
        public LevelObject? LockedObject { get => GetValue(LockedObjectProperty); set => SetValue(LockedObjectProperty, value); }

        /// <summary>Whether desktop hitboxes are drawn over objects.</summary>
        public bool ShowHitboxes { get => GetValue(ShowHitboxesProperty); set => SetValue(ShowHitboxesProperty, value); }

        /// <summary>Whether phone hitboxes are drawn over objects.</summary>
        public bool ShowMobileHitboxes { get => GetValue(ShowMobileHitboxesProperty); set => SetValue(ShowMobileHitboxesProperty, value); }

        /// <summary>Editor-decoration rope skin index applied to every rope (0 = default brown).</summary>
        public int ActiveRopeSkin { get => GetValue(ActiveRopeSkinProperty); set => SetValue(ActiveRopeSkinProperty, value); }

        /// <summary>Editor-decoration background id (0 = none, 1..7 = bgr_01..bgr_07).</summary>
        public int ActiveBackground { get => GetValue(ActiveBackgroundProperty); set => SetValue(ActiveBackgroundProperty, value); }

        /// <summary>Editor-decoration candy skin index applied to candy sprites (0 = default candy).</summary>
        public int ActiveCandySkin { get => GetValue(ActiveCandySkinProperty); set => SetValue(ActiveCandySkinProperty, value); }

        /// <summary>Editor-decoration Om Nom sitting platform index applied to the target (0 = default).</summary>
        public int ActiveOmNomSupport { get => GetValue(ActiveOmNomSupportProperty); set => SetValue(ActiveOmNomSupportProperty, value); }

        /// <summary>Largest horizontal scroll offset in screen pixels.</summary>
        public double HorizontalScrollMaximum { get => GetValue(HorizontalScrollMaximumProperty); private set => SetValue(HorizontalScrollMaximumProperty, value); }

        /// <summary>Largest vertical scroll offset in screen pixels.</summary>
        public double VerticalScrollMaximum { get => GetValue(VerticalScrollMaximumProperty); private set => SetValue(VerticalScrollMaximumProperty, value); }

        /// <summary>Visible canvas width used by the horizontal scrollbar thumb.</summary>
        public double HorizontalScrollViewport { get => GetValue(HorizontalScrollViewportProperty); private set => SetValue(HorizontalScrollViewportProperty, value); }

        /// <summary>Visible canvas height used by the vertical scrollbar thumb.</summary>
        public double VerticalScrollViewport { get => GetValue(VerticalScrollViewportProperty); private set => SetValue(VerticalScrollViewportProperty, value); }

        /// <summary>Current horizontal scroll offset in screen pixels.</summary>
        public double HorizontalScrollValue { get => GetValue(HorizontalScrollValueProperty); set => SetValue(HorizontalScrollValueProperty, value); }

        /// <summary>Current vertical scroll offset in screen pixels.</summary>
        public double VerticalScrollValue { get => GetValue(VerticalScrollValueProperty); set => SetValue(VerticalScrollValueProperty, value); }

        /// <summary>Callback used to place a new object at level coordinates.</summary>
        public Func<string, int, int, LevelObject?>? PlaceAt { get; set; }

        /// <summary>Callback used to toggle the locked object from canvas gestures.</summary>
        public Action<LevelObject?>? ToggleLock { get; set; }

        /// <summary>Callback raised when a canvas drag moves the selected object, so bound views can refresh.</summary>
        public Action? SelectedObjectMoved { get; set; }

        /// <summary>Callback raised before a direct canvas edit begins, so the view model can capture undo state.</summary>
        public Action? BeginDocumentEdit { get; set; }

        /// <summary>Callback raised after a direct canvas edit ends, so the view model can commit undo state.</summary>
        public Action? CompleteDocumentEdit { get; set; }

        private bool _dragging;
        // True while dragging the selected object's rotation dial.
        private bool _rotating;
        private bool _resizingRadius;
        // Which movable-rail handle the current drag is manipulating (slide the hook or resize an end);
        // None when no rail drag is in progress. A MoveBar drag routes through _dragging instead.
        private GrabRail.Handle _railDrag;
        // Whether the pointer is hovering the selected grab's hook, so it shows the highlight art even
        // before a drag begins (the game highlights the mover on interaction).
        private bool _hookHovered;
        // True while the cursor hovers the selected object's rotation knob (lights it up).
        private bool _dialKnobHovered;
        private Vec2 _dragOffset;
        private int _lastHitIndex = -1;
        private bool _panning;
        private Point _panLast;
        private double _lastPinchScale = 1;
        private bool _syncingScroll;
        private bool _pendingFit;
        private bool _ghostActive;
        private string? _ghostElement;
        private Vec2 _ghostLevel;

        // Editor-chrome brushes/pens resolved from the theme once per theme change, not per Render.
        private readonly CanvasPalette _palette = new();

        /// <summary>Creates the canvas and enables native touch gestures.</summary>
        public LevelCanvas()
        {
            GestureRecognizers.Add(new PinchGestureRecognizer());
            AddHandler(PinchEvent, Canvas_Pinch, RoutingStrategies.Bubble);
            AddHandler(PinchEndedEvent, Canvas_PinchEnded, RoutingStrategies.Bubble);
            AddHandler(PointerTouchPadGestureMagnifyEvent, Canvas_TouchPadMagnify, RoutingStrategies.Bubble);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _palette.Refresh(this);
            ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            ActualThemeVariantChanged -= OnActualThemeVariantChanged;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnActualThemeVariantChanged(object? sender, EventArgs e)
        {
            _palette.Refresh(this);
            InvalidateVisual();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DocumentProperty)
            {
                // Auto-fit only when the level's dimensions change (a fresh load, a new level, or a
                // resolution change). An undo/redo restore swaps in a re-parsed same-sized document,
                // and refitting it would throw away the user's current zoom and pan.
                LevelDocument? oldDoc = change.GetOldValue<LevelDocument?>();
                LevelDocument? newDoc = change.GetNewValue<LevelDocument?>();
                if (newDoc is not null
                    && (oldDoc is null || oldDoc.Width != newDoc.Width || oldDoc.Height != newDoc.Height))
                {
                    _pendingFit = true;
                    TryFit(); // fits immediately if already laid out (later loads); else waits for Bounds.
                }
                UpdateScrollState();
            }
            else if (change.Property == BoundsProperty && _pendingFit)
            {
                TryFit();
            }
            else if (change.Property == BoundsProperty || change.Property == ViewProperty)
            {
                UpdateScrollState();
            }
            else if ((change.Property == HorizontalScrollValueProperty || change.Property == VerticalScrollValueProperty)
                     && !_syncingScroll)
            {
                ScrollTo(HorizontalScrollValue, VerticalScrollValue);
            }
        }

    }
}

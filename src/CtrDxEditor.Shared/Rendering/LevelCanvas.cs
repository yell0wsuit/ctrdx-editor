using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Rendering
{
    /// <summary>Interactive editor canvas for rendering, selecting, dragging, zooming, and placing level objects.</summary>
    public sealed partial class LevelCanvas : Control
    {
        /// <summary>Avalonia property backing <see cref="Document"/>.</summary>
        public static readonly StyledProperty<LevelDocument?> DocumentProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelDocument?>(nameof(Document));

        /// <summary>Avalonia property backing <see cref="HiddenObjects"/>.</summary>
        public static readonly StyledProperty<IReadOnlySet<LevelObject>?> HiddenObjectsProperty =
            AvaloniaProperty.Register<LevelCanvas, IReadOnlySet<LevelObject>?>(nameof(HiddenObjects));

        /// <summary>Avalonia property backing <see cref="LockedOutObjects"/>.</summary>
        public static readonly StyledProperty<IReadOnlySet<LevelObject>?> LockedOutObjectsProperty =
            AvaloniaProperty.Register<LevelCanvas, IReadOnlySet<LevelObject>?>(nameof(LockedOutObjects));

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

        /// <summary>Avalonia property backing <see cref="ShowForceFields"/>.</summary>
        public static readonly StyledProperty<bool> ShowForceFieldsProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowForceFields), defaultValue: true);

        /// <summary>Avalonia property backing <see cref="ShowMovementPaths"/>.</summary>
        public static readonly StyledProperty<bool> ShowMovementPathsProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowMovementPaths), defaultValue: true);

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

        /// <summary>Avalonia property backing <see cref="AnimationPreviewMode"/>.</summary>
        public static readonly StyledProperty<AnimationPreviewMode> AnimationPreviewModeProperty =
            AvaloniaProperty.Register<LevelCanvas, AnimationPreviewMode>(nameof(AnimationPreviewMode));

        /// <summary>Avalonia property backing <see cref="AnimationPreviewObject"/>.</summary>
        public static readonly StyledProperty<LevelObject?> AnimationPreviewObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(nameof(AnimationPreviewObject));

        /// <summary>Avalonia property backing <see cref="AnimationPreviewElapsedSeconds"/>.</summary>
        public static readonly StyledProperty<double> AnimationPreviewElapsedSecondsProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(AnimationPreviewElapsedSeconds));

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
            _ = LockedOutObjectsProperty.Changed.AddClassHandler<LevelCanvas>((canvas, _) => canvas.ClearLockedOutInteraction());
            _ = SelectedObjectProperty.Changed.AddClassHandler<LevelCanvas>((canvas, _) => canvas.ClearLockedOutInteraction());
            _ = LockedObjectProperty.Changed.AddClassHandler<LevelCanvas>((canvas, _) => canvas.ClearLockedOutInteraction());
            AffectsRender<LevelCanvas>(
                DocumentProperty, HiddenObjectsProperty, SpritesProperty, ViewProperty, SnapEnabledProperty,
                SelectedObjectProperty, LockedObjectProperty,
                ShowHitboxesProperty, ShowForceFieldsProperty, ShowMovementPathsProperty,
                ActiveRopeSkinProperty, ActiveBackgroundProperty, ActiveCandySkinProperty,
                ActiveOmNomSupportProperty,
                AnimationPreviewModeProperty, AnimationPreviewObjectProperty, AnimationPreviewElapsedSecondsProperty);
        }

        /// <summary>The loaded level document to render and edit.</summary>
        public LevelDocument? Document { get => GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

        /// <summary>Objects excluded from rendering and interaction by layer, object, or locale visibility.</summary>
        public IReadOnlySet<LevelObject>? HiddenObjects { get => GetValue(HiddenObjectsProperty); set => SetValue(HiddenObjectsProperty, value); }

        /// <summary>Objects that render normally but are excluded from hit-testing because their layer is locked.</summary>
        public IReadOnlySet<LevelObject>? LockedOutObjects { get => GetValue(LockedOutObjectsProperty); set => SetValue(LockedOutObjectsProperty, value); }

        /// <summary>Sprite cache used to render object art.</summary>
        public SpriteCache? Sprites { get => GetValue(SpritesProperty); set => SetValue(SpritesProperty, value); }

        /// <summary>Current zoom and pan transform.</summary>
        public ViewTransform View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }

        /// <summary>Whether object moves and placements snap to the level grid.</summary>
        public bool SnapEnabled { get => GetValue(SnapEnabledProperty); set => SetValue(SnapEnabledProperty, value); }

        /// <summary>The currently selected object, if any.</summary>
        public LevelObject? SelectedObject { get => GetValue(SelectedObjectProperty); set => SetValue(SelectedObjectProperty, value); }

        /// <summary>Every selected object. Drives multi-object outlines and group move.</summary>
        public IReadOnlySet<LevelObject> SelectedObjects { get; set; } = new HashSet<LevelObject>();

        /// <summary>The primary selected object; drives specialized handles and the property panel.</summary>
        public LevelObject? PrimaryObject
        {
            get => SelectedObject;
            set => SelectedObject = value;
        }

        /// <summary>True when exactly one object is selected; specialized handles require this.</summary>
        private bool IsSingleSelection => SelectedObjects.Count == 1;

        /// <summary>The object locked for exclusive interaction, if any.</summary>
        public LevelObject? LockedObject { get => GetValue(LockedObjectProperty); set => SetValue(LockedObjectProperty, value); }

        /// <summary>Whether desktop hitboxes are drawn over objects.</summary>
        public bool ShowHitboxes { get => GetValue(ShowHitboxesProperty); set => SetValue(ShowHitboxesProperty, value); }

        /// <summary>Whether directional force-field arrows (e.g. the pump's flow) are drawn over objects.</summary>
        public bool ShowForceFields { get => GetValue(ShowForceFieldsProperty); set => SetValue(ShowForceFieldsProperty, value); }

        /// <summary>Whether object movement path guides are drawn over objects.</summary>
        public bool ShowMovementPaths { get => GetValue(ShowMovementPathsProperty); set => SetValue(ShowMovementPathsProperty, value); }

        /// <summary>Editor-decoration rope skin index applied to every rope (0 = default brown).</summary>
        public int ActiveRopeSkin { get => GetValue(ActiveRopeSkinProperty); set => SetValue(ActiveRopeSkinProperty, value); }

        /// <summary>Editor-decoration background id (0 = none, 1..7 = bgr_01..bgr_07).</summary>
        public int ActiveBackground { get => GetValue(ActiveBackgroundProperty); set => SetValue(ActiveBackgroundProperty, value); }

        /// <summary>Editor-decoration candy skin index applied to candy sprites (0 = default candy).</summary>
        public int ActiveCandySkin { get => GetValue(ActiveCandySkinProperty); set => SetValue(ActiveCandySkinProperty, value); }

        /// <summary>Editor-decoration Om Nom sitting platform index applied to the target (0 = default).</summary>
        public int ActiveOmNomSupport { get => GetValue(ActiveOmNomSupportProperty); set => SetValue(ActiveOmNomSupportProperty, value); }

        /// <summary>Which live object-animation preview is active.</summary>
        public AnimationPreviewMode AnimationPreviewMode { get => GetValue(AnimationPreviewModeProperty); set => SetValue(AnimationPreviewModeProperty, value); }

        /// <summary>The object targeted by object-scoped live preview, or null.</summary>
        public LevelObject? AnimationPreviewObject { get => GetValue(AnimationPreviewObjectProperty); set => SetValue(AnimationPreviewObjectProperty, value); }

        /// <summary>Elapsed live-preview time in seconds.</summary>
        public double AnimationPreviewElapsedSeconds { get => GetValue(AnimationPreviewElapsedSecondsProperty); set => SetValue(AnimationPreviewElapsedSecondsProperty, value); }

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

        /// <summary>How the canvas asks the view model to change selection.</summary>
        public enum SelectionRequestKind
        {
            /// <summary>Replace the current selection.</summary>
            Replace,

            /// <summary>Toggle one object in the current selection.</summary>
            Toggle,

            /// <summary>Clear the current selection.</summary>
            Clear,
        }

        /// <summary>A selection change the canvas requests; the view model applies it to <see cref="EditorSelection"/>.</summary>
        public readonly record struct SelectionRequest(SelectionRequestKind Kind, LevelObject? Target);

        /// <summary>Raised when the canvas wants the view model to change selection.</summary>
        public Action<SelectionRequest>? SelectionRequested { get; set; }

        /// <summary>Callback raised when a canvas drag moves the selected object, so bound views can refresh.</summary>
        public Action? SelectedObjectMoved { get; set; }

        /// <summary>Raised when a command-drag should duplicate the selection in place before moving it.</summary>
        public Action? DuplicateRequested { get; set; }

        /// <summary>Raised with a segment index when a hand joint is pressed, so the panel can expand it.</summary>
        public Action<int>? HandSegmentActivated { get; set; }

        /// <summary>Callback raised (F2) to open the inline text editor over the given tutorial text.</summary>
        public Action<LevelObject>? EditTutorialTextRequested { get; set; }

        /// <summary>
        /// The screen-space rectangle covering a tutorial text's selection box, for positioning the inline
        /// editor overlay. Returns an empty rect when the sprite cache is unavailable.
        /// </summary>
        /// <param name="obj">The tutorial text object.</param>
        /// <returns>The text's bounds in canvas (screen) pixels.</returns>
        public Rect TutorialTextScreenRect(LevelObject obj)
        {
            if (Sprites is not { } sprites)
            {
                return default;
            }

            LevelBounds b = TutorialRenderer.TextBounds(sprites, obj);
            Vec2 tl = View.LevelToScreen(new Vec2(b.X, b.Y));
            Vec2 br = View.LevelToScreen(new Vec2(b.X + b.W, b.Y + b.H));
            return new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
        }

        /// <summary>Callback raised before a direct canvas edit begins, so the view model can capture undo state.</summary>
        public Action? BeginDocumentEdit { get; set; }

        /// <summary>Callback raised after a direct canvas edit ends, so the view model can commit undo state.</summary>
        public Action? CompleteDocumentEdit { get; set; }

        private bool IsHidden(LevelObject obj)
        {
            return HiddenObjects?.Contains(obj) == true;
        }

        private bool IsLockedOut(LevelObject obj)
        {
            return LockedOutObjects?.Contains(obj) == true;
        }

        private void ClearLockedOutInteraction()
        {
            if (SelectedObject is { } selected && IsLockedOut(selected))
            {
                SelectedObject = null;
            }
            if (LockedObject is { } locked && IsLockedOut(locked))
            {
                LockedObject = null;
            }
        }

        /// <summary>True while dragging the selected object (or a grab via its move-bar) to a new position.</summary>
        private bool _dragging;

        /// <summary>Original coordinates for every object participating in the current group drag.</summary>
        private readonly List<(LevelObject Obj, int X, int Y)> _groupDragOrigins = [];

        /// <summary>True while a command-modified press awaits enough pointer travel to become duplication.</summary>
        private bool _pendingDupDrag;

        /// <summary>Pointer origin for the pending command-drag duplicate gesture.</summary>
        private Vec2 _dupDragStart;

        /// <summary>True after a pending command-drag has crossed its threshold and cloned the selection.</summary>
        private bool _dupDragArmed;

        /// <summary>Primary-object coordinates captured with <see cref="_groupDragOrigins"/>.</summary>
        private int _primaryOriginX;
        private int _primaryOriginY;

        /// <summary>True while the water surface line is being dragged.</summary>
        private bool _waterDrag;

        /// <summary>True while the pointer is over the water surface line.</summary>
        private bool _waterHandleHovered;

        /// <summary>True while dragging the selected object's rotation dial.</summary>
        private bool _rotating;

        /// <summary>Stable level-space pivot captured when a rotation-dial gesture begins.</summary>
        private Vec2 _rotationDragCenter;

        /// <summary>True while dragging a grab's auto-catch radius ring to resize it.</summary>
        private bool _resizingRadius;

        /// <summary>True while dragging the selected tutorial text's right edge to change wrap width.</summary>
        private bool _resizingTutorialText;

        /// <summary>Level-space pointer X where a tutorial text width gesture began.</summary>
        private double _tutorialTextResizeStartPointerX;

        /// <summary>Horizontal pointer offset from the tutorial text edge captured at gesture start.</summary>
        private double _tutorialTextResizeGrabOffsetX;

        /// <summary>True after the tutorial text resize gesture crosses its drag threshold.</summary>
        private bool _tutorialTextResizeHasDragged;

        /// <summary>Rotation mapping used only while previewing a ghost's small-bouncer morph.</summary>
        private static readonly RotationSpec GhostBouncerRotation = new(DisplayOffset: 0);

        /// <summary>Ephemeral morph preview for the selected ghost, reset whenever selection changes.</summary>
        private readonly GhostPreviewState _ghostPreview = new();

        /// <summary>Screen-space state-selector hit targets populated while the ghost badge is drawn.</summary>
        private readonly List<(Rect Rect, GhostMorph Morph)> _ghostIconHits = [];

        /// <summary>
        /// Which movable-rail handle the current drag is manipulating (slide the hook or resize an end);
        /// <see cref="GrabRail.Handle.None"/> when no rail drag is in progress. A <see cref="GrabRail.Handle.MoveBar"/>
        /// drag routes through <see cref="_dragging"/> instead.
        /// </summary>
        private GrabRail.Handle _railDrag;

        /// <summary>Which selected spike end is being dragged to choose a new spike size.</summary>
        private SpikeResize.Handle _stripResizeDrag;

        /// <summary>Which selected conveyor handle the current drag is manipulating (far end or width),
        /// or <see cref="ConveyorGeometry.Handle.None"/> when no conveyor drag is in progress.</summary>
        private ConveyorGeometry.Handle _conveyorDrag;

        /// <summary>Which vinyl disc handle the current drag is rotating, or <see cref="VinylGeometry.Handle.None"/>.</summary>
        private VinylGeometry.Handle _vinylHandleDrag;

        /// <summary>Which selected vinyl handle the cursor is hovering, so it shows the game's active-controller
        /// glow and disc ring before a drag begins.</summary>
        private VinylGeometry.Handle _vinylHandleHovered;

        /// <summary>
        /// Whether the pointer is hovering the selected grab's hook, so it shows the highlight art even before a
        /// drag begins (the game highlights the mover on interaction).
        /// </summary>
        private bool _hookHovered;

        /// <summary>True while the cursor hovers the selected object's rotation knob (lights it up).</summary>
        private bool _dialKnobHovered;

        /// <summary>Canonical waypoint currently being dragged, or -1 when no path point drag is active.</summary>
        private int _polylinePointDrag = -1;

        /// <summary>Canonical waypoint currently under the pointer, or -1.</summary>
        private int _polylineHoverPoint = -1;

        /// <summary>True while the pointer is over the append nub of the selected polyline.</summary>
        private bool _polylineNubHot;

        /// <summary>True while hovering the end of a selected polyline that has hit its point cap (shows the limit hint).</summary>
        private bool _polylineAtLimitHint;

        /// <summary>The hand segment whose joint is being dragged, or 0 when no hand joint drag is active.</summary>
        private int _handJointDrag;

        /// <summary>True while the hand's base is being dragged.</summary>
        private bool _handBaseDrag;

        /// <summary>True when the ordinary object-drag path is moving a mechanical hand.</summary>
        private bool _handObjectDrag;

        /// <summary>Pointer position where the current hand body/base/joint gesture began, in level units.</summary>
        private Vec2 _handDragStartPointer;

        /// <summary>Original base or joint position, preserving the pointer's grab offset once a button drag begins.</summary>
        private Vec2 _handDragStartTarget;

        /// <summary>True after the current hand gesture crosses its screen-space drag threshold.</summary>
        private bool _handDragHasMoved;

        /// <summary>The hand joint under the cursor, or 0 when none; drives Delete/Backspace.</summary>
        private int _handHoverJoint;

        /// <summary>The hand segment (bone) under the cursor, or 0 when none; drives the hover tint.</summary>
        private int _handHoverSegment;

        /// <summary>The hand segment whose rotation dial is active, or 0 when no hand segment is active.</summary>
        private int _handActiveSegment;

        /// <summary>
        /// Where an Alt-hover would split a bone: the ghost joint's level position and the rotatable flag it
        /// would inherit, or null when no split is being previewed.
        /// </summary>
        private (Vec2 Position, bool Rotatable)? _handSplitPreview;

        /// <summary>Last pointer position in level units while hovering, so an Alt press can refresh the split preview without a move.</summary>
        private Vec2 _lastHoverLevel;

        /// <summary>Level-space offset from the dragged object's origin to the pointer, held constant during a drag.</summary>
        private Vec2 _dragOffset;

        /// <summary>Index of the last object a click hit, so repeated clicks in one spot cycle through stacked objects.</summary>
        private int _lastHitIndex = -1;

        /// <summary>True while panning the view with the middle button or an empty-space drag.</summary>
        private bool _panning;

        /// <summary>Last pointer position during a pan, in screen pixels.</summary>
        private Point _panLast;

        /// <summary>Cumulative pinch scale from the previous pinch event, used to derive an incremental zoom factor.</summary>
        private double _lastPinchScale = 1;

        /// <summary>Guards scrollbar/<see cref="View"/> sync so a programmatic scroll update doesn't recurse back through property changes.</summary>
        private bool _syncingScroll;

        /// <summary>True when a fit-to-view is queued, waiting for the control to be laid out with non-zero bounds.</summary>
        private bool _pendingFit;

        /// <summary>True while a translucent palette drag preview is being shown.</summary>
        private bool _dragPreviewActive;

        /// <summary>Element id of the palette drag preview, or null when none is active.</summary>
        private string? _dragPreviewElement;

        /// <summary>Snapped level-space position of the drag preview.</summary>
        private Vec2 _dragPreviewLevel;

        /// <summary>Editor-chrome brushes/pens resolved from the theme once per theme change, not per Render.</summary>
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

        /// <summary>Re-resolves the theme-dependent palette and repaints when the active theme variant changes.</summary>
        /// <param name="sender">The control raising the theme-changed event.</param>
        /// <param name="e">Event data (unused).</param>
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
            else if (change.Property == SelectedObjectProperty)
            {
                _ghostPreview.Clear();
                _ghostIconHits.Clear();
                _polylinePointDrag = -1;
                _handActiveSegment = 0;
                _handHoverJoint = 0;
                _handHoverSegment = 0;
                _handSplitPreview = null;
                ResetPolylineHover();
                InvalidateVisual();
            }
        }

        /// <summary>Clears selected-polyline hover chrome and repaints when it was visible.</summary>
        private void ResetPolylineHover()
        {
            bool changed = _polylineHoverPoint != -1 || _polylineNubHot || _polylineAtLimitHint;
            _polylineHoverPoint = -1;
            _polylineNubHot = false;
            _polylineAtLimitHint = false;
            if (changed)
            {
                InvalidateVisual();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;
using CtrDxEditor.Rendering;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Main editor state and commands shared by the window and canvas.</summary>
    /// <param name="sprites">Already-preloaded sprite cache for the active content.</param>
    /// <param name="settings">Persisted editor settings store, or null in tests that don't exercise persistence.</param>
    /// <param name="initial">The editor settings snapshot loaded at startup (decoration defaults, content path).</param>
    public sealed partial class EditorViewModel(SpriteCache sprites, ISettingsStore? settings = null, EditorSettings? initial = null) : ViewModelBase
    {
        private const int UndoHistoryLimit = 100;
        private readonly DescriptorTable _descriptors = DescriptorTable.CtrObjects;
        private readonly List<HistoryState> _undoStack = [];
        private readonly List<HistoryState> _redoStack = [];
        private HistoryState? _pendingUndoTransaction;

        // Serialized level XML as of the last load/new/save. Null when no level is open. Compared against
        // the live document to detect unsaved changes (see IsModified); reusing ToXml keeps the comparison
        // identical to what a save actually writes, so decoration/zoom/selection never count as edits.
        private string? _savedBaselineXml;

        /// <summary>Persisted editor settings store, for reading/writing decoration defaults; null when unavailable.</summary>
        public ISettingsStore? Settings { get; } = settings;

        /// <summary>The last-loaded editor settings snapshot (decoration defaults, content path).</summary>
        public EditorSettings CurrentSettingsSnapshot { get; set; } = initial ?? new EditorSettings();

        [ObservableProperty] public partial LevelDocument? Document { get; set; }
        [ObservableProperty] public partial ViewTransform View { get; set; } = ViewTransform.Identity;
        [ObservableProperty] public partial LevelObject? SelectedObject { get; set; }
        [ObservableProperty] public partial LevelObject? LockedObject { get; set; }
        [ObservableProperty] public partial bool SnapEnabled { get; set; }
        [ObservableProperty] public partial bool ShowHitboxes { get; set; } = true;
        [ObservableProperty] public partial bool ShowForceFields { get; set; } = true;
        [ObservableProperty] public partial bool ShowMovementPaths { get; set; } = true;
        [ObservableProperty] public partial int ActiveRopeSkin { get; set; }
        [ObservableProperty] public partial int ActiveBackground { get; set; }
        [ObservableProperty] public partial int ActiveCandySkin { get; set; }
        [ObservableProperty] public partial int ActiveOmNomSupport { get; set; }
        [ObservableProperty] public partial AnimationPreviewMode AnimationPreviewMode { get; set; }
        [ObservableProperty] public partial LevelObject? AnimationPreviewObject { get; set; }
        [ObservableProperty] public partial double AnimationPreviewElapsedSeconds { get; set; }
        [ObservableProperty] public partial int ObjectListVersion { get; set; }
        [ObservableProperty] public partial string PaletteSearchText { get; set; } = "";

        /// <summary>The layer that receives newly placed objects, or null when the level has none.</summary>
        [ObservableProperty] public partial LayerViewModel? ActiveLayer { get; set; }

        /// <summary>The locale whose localized objects are shown. Session-only.</summary>
        [ObservableProperty] public partial string DisplayLocale { get; set; } = "en";

        // Session-only visibility keyed by XML identity (objects) and layer name (layers).
        private readonly HashSet<XElement> _hiddenObjectElements = [];
        private readonly HashSet<string> _hiddenLayerNames = [];

        /// <summary>Sprite cache for the active content.</summary>
        public SpriteCache Sprites { get; } = sprites;

        /// <summary>Palette items available for placement.</summary>
        public ObservableCollection<PaletteItemViewModel> Palette { get; } = [];

        /// <summary>Palette items after applying <see cref="PaletteSearchText"/>.</summary>
        public ObservableCollection<PaletteItemViewModel> PaletteView { get; } = [];

        /// <summary>Attribute fields for the selected object.</summary>
        public ObservableCollection<AttributeFieldViewModel> Fields { get; } = [];

        /// <summary>
        /// <see cref="Fields"/> partitioned into panel sections. Consecutive fields sharing a group are drawn
        /// together; fields with no group land in an anonymous section that renders bare. The panel binds to
        /// this, while <see cref="Fields"/> remains the flat source of truth for field builders.
        /// </summary>
        public ObservableCollection<PropertyGroupViewModel> FieldGroups { get; } = [];

        /// <summary>Layer rows for the object panel tree.</summary>
        public ObservableCollection<LayerViewModel> Layers { get; } = [];

        /// <summary>Locales available in the current level, with English first.</summary>
        public ObservableCollection<string> AvailableLocales { get; } = [];

        /// <summary>Objects currently hidden by layer or individual visibility settings.</summary>
        public IReadOnlySet<LevelObject> EffectivelyHiddenObjects { get; private set; } = new HashSet<LevelObject>();

        /// <summary>Raised when a selected object's editable values change.</summary>
        public event Action? ObjectMutated;

        /// <summary>Raised after a level XML document has loaded into the editor.</summary>
        public event Action? LevelLoaded;

        /// <summary>The current level's editable settings, or null when no level is loaded.</summary>
        public LevelSettings? CurrentSettings => Document?.Settings;

        /// <summary>True when a level is open and editor-only commands can run.</summary>
        public bool HasDocument => Document is not null;

        /// <summary>
        /// True when the open level has edits that differ from the last load, new, or save. Undoing all the
        /// way back to the saved state clears it; decoration, zoom, and selection changes never set it.
        /// </summary>
        public bool IsModified => Document is not null && ToXml() != _savedBaselineXml;

        /// <summary>Marks the current document as saved, so it no longer counts as modified until the next edit.</summary>
        public void MarkSaved()
        {
            _savedBaselineXml = ToXml();
        }

        /// <summary>Whether the selected object has real polyline movement with direct-edit handles.</summary>
        public bool CanEditPolyline => SelectedObject is { } obj
            && MoverPath.IsPolylineMovement(obj.GetAttr("path"));

        /// <summary>True when an undo snapshot is available.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>True when a redo snapshot is available.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>True when live object-animation preview is currently running.</summary>
        public bool IsAnimationPreviewActive => AnimationPreviewMode != AnimationPreviewMode.Off;

        /// <summary>View-menu label for starting or stopping live object-animation preview.</summary>
        public string AnimationPreviewMenuText => Localizer.Get(
            IsAnimationPreviewActive ? "Menu.View.StopAnimations" : "Menu.View.PlayAnimations");

        /// <summary>Loads a level from its XML text into the editor.</summary>
        public void LoadLevelXml(string xml)
        {
            StopAnimationPreview();
            Document = LevelDocument.Parse(xml);
            LevelObjectPolicy.NormalizeBindingKeys(Document);
            SelectedObject = null;
            LockedObject = null;
            ClearHistory();
            // The canvas fits the level to the viewport once it is laid out (LevelCanvas.FitToView).
            RefreshPalette();
            RefreshObjectList();
            RefreshLocales();
            // Baseline captured before size normalization so a level whose spike/bouncer tags
            // disagree with their size attribute loads as a pending (savable) change, while a
            // consistent level stays clean. See LevelObjectPolicy.NormalizeSizedElements.
            _savedBaselineXml = ToXml();
            bool normalized = LevelObjectPolicy.NormalizeSizedElements(Document);
            // The legacy `mouse` tag is an alias for `gap` (same game loader), so it loads renamed
            // to `gap` and pending save, matching what the game would load.
            normalized |= LevelObjectPolicy.NormalizeMouseAlias(Document);
            // The game truncates x/y (and gameDesign mapOffsetX/Y) to ints, so a level authored
            // with decimals loads auto-fixed and pending save, matching what the game would load.
            normalized |= LevelObjectPolicy.DropCoordinateDecimals(Document);
            if (normalized)
            {
                RefreshObjectList();
            }
            LevelLoaded?.Invoke();
        }

        /// <summary>Closes the current level and clears document-scoped editor state.</summary>
        public void CloseLevel()
        {
            StopAnimationPreview();
            Document = null;
            SelectedObject = null;
            LockedObject = null;
            _savedBaselineXml = null;
            ClearHistory();
            Palette.Clear();
            RebuildPaletteView();
            Layers.Clear();
            ActiveLayer = null;
            _hiddenObjectElements.Clear();
            _hiddenLayerNames.Clear();
            EffectivelyHiddenObjects = new HashSet<LevelObject>();
            AvailableLocales.Clear();
            Fields.Clear();
            FieldGroups.Clear();
        }

        /// <summary>
        /// Restores the active editor decoration from the persisted snapshot at startup.
        /// Random/Blank (ids &lt;= 0) fall back to the plain defaults on open.
        /// </summary>
        public void InitializeDecorationFromSettings()
        {
            ActiveRopeSkin = CurrentSettingsSnapshot.RopeSkin >= 0 ? CurrentSettingsSnapshot.RopeSkin : 0;
            ActiveBackground = CurrentSettingsSnapshot.Background > 0 ? CurrentSettingsSnapshot.Background : 0;
            ActiveCandySkin = CurrentSettingsSnapshot.CandySkin >= 0 ? CurrentSettingsSnapshot.CandySkin : 0;
            ActiveOmNomSupport = CurrentSettingsSnapshot.OmNomSupport >= 0 ? CurrentSettingsSnapshot.OmNomSupport : 0;
        }

        /// <summary>Creates a new empty level from the given settings and applies the chosen editor decoration.</summary>
        public void NewLevel(LevelSettings settings, int ropeSkin = 0, int background = 0, int candySkin = 0, int omNomSupport = 0)
        {
            StopAnimationPreview();
            Document = LevelDocument.CreateNew(settings);
            ActiveRopeSkin = ropeSkin;
            ActiveBackground = background;
            ActiveCandySkin = candySkin;
            ActiveOmNomSupport = omNomSupport;
            SelectedObject = null;
            LockedObject = null;
            ClearHistory();
            RefreshPalette();
            RefreshObjectList();
            RefreshLocales();
            _savedBaselineXml = ToXml();
            LevelLoaded?.Invoke();
        }

        /// <summary>Writes edited settings back into the current level and refreshes the view.</summary>
        public void UpdateLevelSettings(LevelSettings settings)
        {
            if (Document is null)
            {
                return;
            }
            StopAnimationPreview();
            CaptureUndoSnapshot();
            Document.UpdateSettings(settings);
            LevelObjectPolicy.NormalizeBindingKeys(Document);
            RefreshPalette();
            RefreshObjectList();
            if (SelectedObject is not null && !Document.AllObjects.Contains(SelectedObject))
            {
                SelectedObject = null;
            }
            if (LockedObject is not null && !Document.AllObjects.Contains(LockedObject))
            {
                LockedObject = null;
            }
            // Resolution may have changed; re-fit and repaint the canvas.
            LevelLoaded?.Invoke();
            ObjectMutated?.Invoke();
        }

        /// <summary>Deletes the currently selected object, if one exists.</summary>
        public void DeleteSelected()
        {
            if (SelectedObject is { } selected)
            {
                Delete(selected);
            }
        }

        /// <summary>Removes <paramref name="removed"/> from the document, capturing undo and refreshing views.</summary>
        /// <param name="removed">The object to delete.</param>
        public void Delete(LevelObject removed)
        {
            if (Document is null)
            {
                return;
            }

            if (IsAnimationPreviewing(removed))
            {
                StopAnimationPreview();
            }
            CaptureUndoSnapshot();
            LevelDocument.Remove(removed);
            LevelObjectPolicy.NormalizeBindingKeys(Document);
            if (Equals(LockedObject, removed))
            {
                LockedObject = null;
            }
            if (Equals(SelectedObject, removed))
            {
                SelectedObject = null;
            }
            RefreshPalette();
            RefreshObjectList();
        }

        /// <summary>Pins (or unpins) an object so canvas clicks won't fall through to overlapping objects.</summary>
        public void ToggleLock(LevelObject? obj)
        {
            if (obj is null)
            {
                LockedObject = null;
                return;
            }

            LockedObject = Equals(LockedObject, obj) ? null : obj;
            if (LockedObject is not null)
            {
                SelectedObject = obj;
            }
        }

        /// <summary>Starts live preview for every spinning object, resetting elapsed time to zero.</summary>
        public void PlayAllAnimations()
        {
            AnimationPreviewObject = null;
            AnimationPreviewElapsedSeconds = 0;
            AnimationPreviewMode = AnimationPreviewMode.All;
        }

        /// <summary>Stops live preview and resets visual playback to the authored static state.</summary>
        public void StopAnimationPreview()
        {
            AnimationPreviewMode = AnimationPreviewMode.Off;
            AnimationPreviewObject = null;
            AnimationPreviewElapsedSeconds = 0;
        }

        /// <summary>Toggles the View-menu playback command: any active preview stops; otherwise all objects play.</summary>
        public void ToggleAnimationPreviewAll()
        {
            if (IsAnimationPreviewActive)
            {
                StopAnimationPreview();
                return;
            }
            PlayAllAnimations();
        }

        /// <summary>Starts live preview for one object, or stops when that same object is already the object preview.</summary>
        /// <param name="obj">Object to preview.</param>
        public void ToggleAnimationPreviewObject(LevelObject obj)
        {
            if (AnimationPreviewMode == AnimationPreviewMode.Focused && Equals(AnimationPreviewObject, obj))
            {
                StopAnimationPreview();
                return;
            }

            AnimationPreviewObject = obj;
            AnimationPreviewElapsedSeconds = 0;
            AnimationPreviewMode = AnimationPreviewMode.Focused;
        }

        /// <summary>Whether live preview motion applies to the given object right now.</summary>
        /// <param name="obj">Object to inspect.</param>
        /// <returns>True when global preview is active, or when object-scoped preview targets <paramref name="obj"/>.</returns>
        public bool IsAnimationPreviewing(LevelObject obj)
        {
            return AnimationPreviewMode == AnimationPreviewMode.All
                || (AnimationPreviewMode == AnimationPreviewMode.Focused && Equals(AnimationPreviewObject, obj));
        }

        /// <summary>Rebuilds the layer tree from the current document, preserving active-layer and visibility state.</summary>
        public void RefreshObjectList()
        {
            XElement? activeElement = ActiveLayer?.Layer.Element;
            Layers.Clear();
            if (Document is null)
            {
                ActiveLayer = null;
                return;
            }

            foreach (LevelLayer layer in Document.Layers)
            {
                LayerViewModel row = new(layer)
                {
                    IsVisible = !_hiddenLayerNames.Contains(layer.Name),
                };
                foreach (LevelObject obj in layer.Objects)
                {
                    row.Objects.Add(obj);
                }
                Layers.Add(row);
            }

            ActiveLayer = Layers.FirstOrDefault(row => ReferenceEquals(row.Layer.Element, activeElement))
                ?? Layers.FirstOrDefault();
            SyncActiveFlags();
            RecomputeHiddenObjects();
        }

        /// <summary>Whether an object is individually hidden.</summary>
        /// <param name="obj">The object to inspect.</param>
        /// <returns>True when the object is hidden independently of its layer.</returns>
        public bool IsObjectHidden(LevelObject obj)
        {
            return _hiddenObjectElements.Contains(obj.Element);
        }

        /// <summary>Shows or hides a single object.</summary>
        /// <param name="obj">The object to update.</param>
        /// <param name="hidden">Whether the object should be hidden.</param>
        public void SetObjectHidden(LevelObject obj, bool hidden)
        {
            _ = hidden ? _hiddenObjectElements.Add(obj.Element) : _hiddenObjectElements.Remove(obj.Element);
            RecomputeHiddenObjects();
            ObjectMutated?.Invoke();
        }

        /// <summary>Whether a layer is hidden.</summary>
        /// <param name="layer">The layer to inspect.</param>
        /// <returns>True when the layer is hidden.</returns>
        public bool IsLayerHidden(LevelLayer layer)
        {
            return _hiddenLayerNames.Contains(layer.Name);
        }

        /// <summary>Shows or hides an entire layer.</summary>
        /// <param name="layer">The layer to update.</param>
        /// <param name="hidden">Whether the layer should be hidden.</param>
        public void SetLayerHidden(LevelLayer layer, bool hidden)
        {
            _ = hidden ? _hiddenLayerNames.Add(layer.Name) : _hiddenLayerNames.Remove(layer.Name);

            if (Layers.FirstOrDefault(row => ReferenceEquals(row.Layer.Element, layer.Element)) is { } row)
            {
                row.IsVisible = !hidden;
            }
            RecomputeHiddenObjects();
            ObjectMutated?.Invoke();
        }

        private void RecomputeHiddenObjects()
        {
            HashSet<LevelObject> hiddenObjects = [];
            if (Document is null)
            {
                EffectivelyHiddenObjects = hiddenObjects;
                OnPropertyChanged(nameof(EffectivelyHiddenObjects));
                return;
            }

            foreach (LevelLayer layer in Document.Layers)
            {
                bool layerHidden = _hiddenLayerNames.Contains(layer.Name);
                foreach (LevelObject obj in layer.Objects)
                {
                    if (layerHidden || _hiddenObjectElements.Contains(obj.Element) || IsLocaleHidden(obj))
                    {
                        _ = hiddenObjects.Add(obj);
                    }
                }
            }
            EffectivelyHiddenObjects = hiddenObjects;
            OnPropertyChanged(nameof(EffectivelyHiddenObjects));
        }

        private bool IsLocaleHidden(LevelObject obj)
        {
            return obj.GetAttr("locale") is { } locale && locale != DisplayLocale;
        }

        private void RefreshLocales()
        {
            AvailableLocales.Clear();
            AvailableLocales.Add("en");
            if (Document is not null)
            {
                foreach (string locale in Document.AllObjects
                    .Select(obj => obj.GetAttr("locale"))
                    .OfType<string>()
                    .Where(locale => locale != "en")
                    .Distinct()
                    .OrderBy(locale => locale, StringComparer.Ordinal))
                {
                    AvailableLocales.Add(locale);
                }
            }

            if (!AvailableLocales.Contains(DisplayLocale))
            {
                DisplayLocale = "en";
            }
        }

        /// <summary>Adds a uniquely named empty layer and makes it active.</summary>
        [RelayCommand]
        public void AddLayer()
        {
            if (Document is null)
            {
                return;
            }

            CaptureUndoSnapshot();
            _ = Document.AddLayer(UniqueLayerName("Layer"));
            RefreshPalette();
            RefreshObjectList();
            ActiveLayer = Layers.Count > 0 ? Layers[^1] : null;
        }

        /// <summary>Deletes the active layer and every object inside it.</summary>
        [RelayCommand]
        public void DeleteActiveLayer()
        {
            if (Document is null || ActiveLayer is null)
            {
                return;
            }

            CaptureUndoSnapshot();
            LevelLayer removed = ActiveLayer.Layer;
            if (ReferenceEquals(SelectedObject?.Element.Parent, removed.Element))
            {
                SelectedObject = null;
            }
            if (ReferenceEquals(LockedObject?.Element.Parent, removed.Element))
            {
                LockedObject = null;
            }
            Document.RemoveLayer(removed);
            _ = _hiddenLayerNames.Remove(removed.Name);
            RefreshPalette();
            RefreshObjectList();
        }

        /// <summary>Renames a layer when the new name is nonblank and unique.</summary>
        /// <param name="layer">The layer to rename.</param>
        /// <param name="name">The candidate name.</param>
        /// <returns>True when the rename was applied.</returns>
        public bool RenameLayer(LevelLayer layer, string name)
        {
            if (Document is null || !Document.IsLayerNameAvailable(name, layer))
            {
                return false;
            }

            CaptureUndoSnapshot();
            bool wasHidden = _hiddenLayerNames.Remove(layer.Name);
            layer.Rename(name);
            if (wasHidden)
            {
                _ = _hiddenLayerNames.Add(name);
            }
            RefreshObjectList();
            return true;
        }

        /// <summary>Moves the active layer earlier or later in draw order.</summary>
        /// <param name="delta">Positions to shift; negative moves earlier, positive later.</param>
        public void MoveActiveLayer(int delta)
        {
            if (Document is null || ActiveLayer is null)
            {
                return;
            }

            CaptureUndoSnapshot();
            Document.MoveLayer(ActiveLayer.Layer, delta);
            RefreshObjectList();
            ObjectMutated?.Invoke();
        }

        /// <summary>Moves an object into another layer.</summary>
        /// <param name="obj">The object to move.</param>
        /// <param name="target">The destination layer.</param>
        public void MoveObjectToLayer(LevelObject obj, LevelLayer target)
        {
            if (Document is null)
            {
                return;
            }

            CaptureUndoSnapshot();
            bool wasSelected = Equals(SelectedObject, obj);
            Document.MoveObject(obj, target);
            RefreshPalette();
            RefreshObjectList();
            if (wasSelected)
            {
                SelectedObject = obj;
            }
            ObjectMutated?.Invoke();
        }

        private void SyncActiveFlags()
        {
            foreach (LayerViewModel row in Layers)
            {
                row.IsActive = ReferenceEquals(row, ActiveLayer);
            }
        }

        private string UniqueLayerName(string baseName)
        {
            string name = baseName;
            for (int i = 1; Document is { } document && !document.IsLayerNameAvailable(name); i++)
            {
                name = $"{baseName} {i}";
            }
            return name;
        }

        partial void OnActiveLayerChanged(LayerViewModel? value)
        {
            SyncActiveFlags();
        }

        partial void OnDisplayLocaleChanged(string value)
        {
            RecomputeHiddenObjects();
            if (SelectedObject is { } selected && EffectivelyHiddenObjects.Contains(selected))
            {
                SelectedObject = null;
            }
            ObjectMutated?.Invoke();
        }

        /// <summary>Refreshes palette availability from descriptor cardinality and loaded objects.</summary>
        public void RefreshPalette()
        {
            IReadOnlyList<LevelObject> objs = Document?.AllObjects ?? [];
            Palette.Clear();
            foreach (ObjectDescriptor d in _descriptors.ByElement.Values)
            {
                if (Document is not null && !IsAvailableInLevel(d.ElementName, Document))
                {
                    continue;
                }
                bool enabled = Document is not null && LockedObject is null && !Cardinality.IsAtCapacity(d, objs);
                Palette.Add(new PaletteItemViewModel(
                    d.ElementName, Localizer.ObjectName(d.ElementName), enabled,
                    Sprites.GetThumbnail(PaletteSpriteKey(d.ElementName), ActiveCandySkin, ActiveOmNomSupport),
                    GroupLabel(d.Game)));
            }
            RebuildPaletteView();
        }

        partial void OnPaletteSearchTextChanged(string value)
        {
            RebuildPaletteView();
        }

        /// <summary>
        /// Repopulates <see cref="PaletteView"/> from <see cref="Palette"/> and the search text,
        /// matching against both the display name and the raw XML element name. The first visible item
        /// of each group carries a section header; groups after the first also carry a divider.
        /// </summary>
        public void RebuildPaletteView()
        {
            PaletteView.Clear();
            string needle = PaletteSearchText?.Trim() ?? "";
            string? currentGroup = null;
            bool firstGroup = true;
            foreach (PaletteItemViewModel item in Palette)
            {
                if (needle.Length != 0
                    && !item.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    && !item.Element.Contains(needle, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool newGroup = item.GroupName != currentGroup;
                item.ShowGroupHeader = newGroup;
                item.ShowDivider = newGroup && !firstGroup;
                if (newGroup)
                {
                    currentGroup = item.GroupName;
                    firstGroup = false;
                }
                PaletteView.Add(item);
            }
        }

        /// <summary>Localized palette section label for a descriptor's Game, falling back to the raw value.</summary>
        private static string GroupLabel(string game)
        {
            string key = game switch
            {
                "Cut the Rope" => "Palette.Group.CutTheRope",
                "Cut the Rope: Experiments" => "Palette.Group.Experiments",
                _ => "",
            };
            if (key.Length == 0)
            {
                return game;
            }
            string localized = Localizer.Get(key);
            return localized == key ? game : localized;
        }

        private static string PaletteSpriteKey(string element)
        {
            return PaletteSpriteKey(element, SpecialEvents.IsXmas);
        }

        private static string PaletteSpriteKey(string element, bool isXmas)
        {
            return element == "sock" && isXmas ? "sock_xmas" : element;
        }

        // Candy type follows twoParts. When no document is
        // loaded, everything is shown (disabled) so the palette isn't empty on startup.
        private static bool IsAvailableInLevel(string element, LevelDocument doc)
        {
            return element switch
            {
                "candy" => !doc.TwoParts,
                "candyL" or "candyR" => doc.TwoParts,
                "spike2" or "spike3" or "spike4" or "bouncer2"
                    or "tutorial02" or "tutorial03" or "tutorial04" or "tutorial05" or "tutorial06"
                    or "tutorial07" or "tutorial08" or "tutorial09" or "tutorial10" or "tutorial11" => false,
                _ => true,
            };
        }

        /// <summary>Places a new object if the descriptor exists and capacity allows it.</summary>
        public LevelObject? PlaceObject(string element, int levelX, int levelY)
        {
            ObjectDescriptor? d = _descriptors.For(element);
            if (d is null || Document is null || LockedObject is not null || Cardinality.IsAtCapacity(d, Document.AllObjects))
            {
                return null;
            }

            CaptureUndoSnapshot();
            LevelObject obj = Placement.CreateObject(d, levelX, levelY);
            LevelObjectPolicy.ApplyDefaults(obj, Document);
            if (TutorialObject.IsText(obj.Type))
            {
                TutorialRenderer.ApplyAutoWidth(Sprites, obj);
            }
            LevelLayer target = ActiveLayer?.Layer ?? Document.AddLayer("Objects");
            Document.Add(obj, target);
            LevelObjectPolicy.NormalizeBindingKeys(Document);
            RefreshPalette();
            RefreshObjectList();
            SelectedObject = obj;
            return obj;
        }

        /// <summary>Serializes the current level to XML text, or null when no document is loaded.</summary>
        public string? ToXml()
        {
            if (Document is not null)
            {
                LevelObjectPolicy.NormalizeBindingKeys(Document);
            }
            return Document?.Save();
        }

        /// <summary>Begins a coalesced undo transaction for direct document mutations such as canvas drags.</summary>
        public void BeginUndoTransaction()
        {
            _pendingUndoTransaction ??= CreateHistoryState();
        }

        /// <summary>Completes a coalesced undo transaction if the document changed.</summary>
        public void CompleteUndoTransaction()
        {
            if (_pendingUndoTransaction is not { } before || Document is null)
            {
                _pendingUndoTransaction = null;
                return;
            }

            _pendingUndoTransaction = null;
            HistoryState after = CreateHistoryState()!;
            if (HistoryStatesEqual(before, after))
            {
                return;
            }

            PushUndoState(before);
        }

        /// <summary>Restores the previous document snapshot, if available.</summary>
        public void Undo()
        {
            if (Document is null || _undoStack.Count == 0)
            {
                return;
            }

            HistoryState current = CreateHistoryState()!;
            HistoryState previous = PopLast(_undoStack);
            _redoStack.Add(current);
            RestoreHistoryState(previous);
            NotifyHistoryChanged();
        }

        /// <summary>Restores the next document snapshot after an undo, if available.</summary>
        public void Redo()
        {
            if (Document is null || _redoStack.Count == 0)
            {
                return;
            }

            HistoryState current = CreateHistoryState()!;
            HistoryState next = PopLast(_redoStack);
            _undoStack.Add(current);
            RestoreHistoryState(next);
            NotifyHistoryChanged();
        }

        /// <summary>Re-reads every property field from the selected object, for canvas-driven mutations like dragging.</summary>
        public void RefreshFieldValues()
        {
            if (SelectedObject is { } hand && HandObject.IsHand(hand.Type)
                && FieldGroups.Count(group => group.Index > 0) != HandObject.SegmentCount(hand))
            {
                PopulateFields(hand);
                RebuildFieldGroups();
                return;
            }

            if (SelectedObject is { } selected && TutorialObject.IsText(selected.Type))
            {
                bool shouldShowWidth = !TutorialObject.IsAutoWidth(selected);
                bool showsWidth = Fields.Any(field => field.Name == "width");
                if (showsWidth != shouldShowWidth)
                {
                    PopulateFields(selected);
                    RebuildFieldGroups();
                    return;
                }
            }

            foreach (AttributeFieldViewModel field in Fields)
            {
                field.Refresh();
            }
        }

        /// <summary>Partitions fields into sections by their consecutive group tags.</summary>
        /// <param name="fields">The flat field list, in panel order.</param>
        /// <returns>One section per run of fields sharing a group tag.</returns>
        public static IEnumerable<PropertyGroupViewModel> GroupFields(IEnumerable<AttributeFieldViewModel> fields)
        {
            List<PropertyGroupViewModel> groups = [];
            PropertyGroupViewModel? current = null;
            foreach (AttributeFieldViewModel field in fields)
            {
                if (current is null || current.Header != field.GroupHeader || current.Index != field.GroupIndex)
                {
                    current = new PropertyGroupViewModel(field.GroupHeader, field.GroupIndex);
                    groups.Add(current);
                }
                current.Fields.Add(field);
            }
            return groups;
        }

        /// <summary>Expands the section with the given identity, leaving the others untouched.</summary>
        /// <param name="index">The section identity to expand; ignored when no section matches.</param>
        public void ExpandFieldGroup(int index)
        {
            foreach (PropertyGroupViewModel group in FieldGroups)
            {
                if (group.Index == index)
                {
                    group.IsExpanded = true;
                }
            }
        }

        private void RebuildFieldGroups()
        {
            FieldGroups.Clear();
            foreach (PropertyGroupViewModel group in GroupFields(Fields))
            {
                FieldGroups.Add(group);
            }
        }

        partial void OnSelectedObjectChanged(LevelObject? value)
        {
            OnPropertyChanged(nameof(CanEditPolyline));
            PopulateFields(value);
            RebuildFieldGroups();
        }

        /// <summary>
        /// Commits inline-edited tutorial text: captures undo, writes the text, re-syncs auto-width, and
        /// refreshes the panel and canvas. A no-op when the text is unchanged.
        /// </summary>
        /// <param name="obj">The tutorial text being edited.</param>
        /// <param name="newText">The new literal text.</param>
        public void CommitTutorialText(LevelObject obj, string newText)
        {
            // An empty tutorial text is useless (renders nothing), so committing empty removes it.
            if (string.IsNullOrWhiteSpace(newText))
            {
                Delete(obj);
                return;
            }

            if ((obj.GetAttr("text") ?? string.Empty) == newText)
            {
                return;
            }

            CaptureUndoSnapshot();
            obj.SetAttr("text", newText);
            TutorialRenderer.ApplyAutoWidth(Sprites, obj);
            ObjectListVersion++;
            ObjectMutated?.Invoke();
            RefreshFieldValues();
        }

        partial void OnDocumentChanged(LevelDocument? value)
        {
            OnPropertyChanged(nameof(HasDocument));
        }

        partial void OnAnimationPreviewModeChanged(AnimationPreviewMode value)
        {
            OnPropertyChanged(nameof(IsAnimationPreviewActive));
            OnPropertyChanged(nameof(AnimationPreviewMenuText));
        }

        partial void OnLockedObjectChanged(LevelObject? value)
        {
            RefreshPalette();
        }

        // The candy skin changes the candy sprites, so the palette thumbnails must be rebuilt. (The
        // canvas repaints on its own via LevelCanvas's affectsRender binding.)
        partial void OnActiveCandySkinChanged(int value)
        {
            RefreshPalette();
        }

        // The sitting platform changes the target sprite, so the palette's Om Nom thumbnail must rebuild.
        partial void OnActiveOmNomSupportChanged(int value)
        {
            RefreshPalette();
        }

        // Central field construction; re-invoked when a structural grab toggle changes so
        // disclosure and gating re-evaluate.
        private void PopulateFields(LevelObject? value)
        {
            Fields.Clear();
            if (value is null)
            {
                return;
            }

            void Changed()
            {
                ObjectListVersion++;
                ObjectMutated?.Invoke();
            }

            void Changing()
            {
                CaptureUndoSnapshot();
            }

            Fields.Add(new AttributeFieldViewModel(value, "x", AttrType.Whole, null, Changed, Changing));
            Fields.Add(new AttributeFieldViewModel(value, "y", AttrType.Whole, null, Changed, Changing));

            if (TutorialObject.IsText(value.Type) || TutorialObject.IsImage(value.Type))
            {
                TutorialFieldBuilder.Build(Fields, value, Sprites, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "star")
            {
                StarFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "rocket")
            {
                RocketFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "grab" && Document is not null)
            {
                GrabFieldBuilder.Build(Fields, value, Document, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "lantern")
            {
                LanternFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (SpikeObject.IsSpike(value.Type))
            {
                SpikeFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (BouncerObject.IsBouncer(value.Type))
            {
                BouncerFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "ghost")
            {
                GhostFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (HandObject.IsHand(value.Type))
            {
                HandFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (value.Type == "transporter")
            {
                ConveyorFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
                return;
            }

            if (AntPath.IsAnts(value.Type))
            {
                AntFieldBuilder.Build(Fields, value, Changed, Changing);
                return;
            }

            ObjectDescriptor? d = _descriptors.For(value.Type);
            if (d is not null)
            {
                foreach (AttributeSpec spec in d.Attributes)
                {
                    if (Document is not null && !LevelObjectPolicy.IsAttributeVisible(value.Type, spec.Name, Document))
                    {
                        continue;
                    }
                    Fields.Add(new AttributeFieldViewModel(
                        value,
                        spec.Name,
                        spec.Type,
                        spec.EnumValues,
                        Changed,
                        Changing,
                        spec.LocalizationName));
                }

                SpinFieldBuilder.Build(Fields, value, Changed, Changing, () => { PopulateFields(value); RebuildFieldGroups(); });
            }
        }

        private void CaptureUndoSnapshot()
        {
            if (CreateHistoryState() is { } state)
            {
                PushUndoState(state);
            }
        }

        private void PushUndoState(HistoryState state)
        {
            if (_undoStack.Count > 0 && HistoryStatesEqual(_undoStack[^1], state))
            {
                return;
            }

            _undoStack.Add(state);
            if (_undoStack.Count > UndoHistoryLimit)
            {
                _undoStack.RemoveAt(0);
            }

            _redoStack.Clear();
            NotifyHistoryChanged();
        }

        private HistoryState? CreateHistoryState()
        {
            if (Document is null)
            {
                return null;
            }

            ObjectRef[] autoWidthRefs = [.. Document.AllObjects
                .Where(TutorialObject.IsAutoWidth)
                .Select(Document.RefOf)
                .Where(reference => reference is not null)
                .Select(reference => reference!.Value)];
            ObjectRef[] hiddenRefs = [.. Document.AllObjects
                .Where(obj => _hiddenObjectElements.Contains(obj.Element))
                .Select(Document.RefOf)
                .Where(reference => reference is not null)
                .Select(reference => reference!.Value)];
            return new HistoryState(
                Document.Save(),
                SelectedObject is { } selected ? Document.RefOf(selected) : null,
                LockedObject is { } locked ? Document.RefOf(locked) : null,
                autoWidthRefs,
                hiddenRefs);
        }

        private void RestoreHistoryState(HistoryState state)
        {
            Document = LevelDocument.Parse(state.Xml);
            _hiddenObjectElements.Clear();
            foreach (ObjectRef reference in state.HiddenRefs)
            {
                if (Document.Resolve(reference) is { } hidden)
                {
                    _ = _hiddenObjectElements.Add(hidden.Element);
                }
            }
            foreach (ObjectRef reference in state.AutoWidthRefs)
            {
                if (Document.Resolve(reference) is { } obj && TutorialObject.IsText(obj.Type))
                {
                    TutorialObject.SetAutoWidth(obj, true);
                }
            }
            RefreshPalette();
            RefreshObjectList();
            RefreshLocales();
            SelectedObject = state.SelectedRef is { } selectedRef ? Document.Resolve(selectedRef) : null;
            LockedObject = state.LockedRef is { } lockedRef ? Document.Resolve(lockedRef) : null;
            // A restore repaints in place; it must not refit/refocus the canvas the way opening a
            // level does (LevelLoaded), or every undo/redo would throw away the user's zoom and pan.
            ObjectMutated?.Invoke();
        }

        private static bool HistoryStatesEqual(HistoryState left, HistoryState right)
        {
            return left.Xml == right.Xml
                && left.AutoWidthRefs.SequenceEqual(right.AutoWidthRefs)
                && left.HiddenRefs.SequenceEqual(right.HiddenRefs);
        }

        private static HistoryState PopLast(List<HistoryState> states)
        {
            int index = states.Count - 1;
            HistoryState state = states[index];
            states.RemoveAt(index);
            return state;
        }

        private void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _pendingUndoTransaction = null;
            NotifyHistoryChanged();
        }

        private void NotifyHistoryChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private sealed record HistoryState(
            string Xml,
            ObjectRef? SelectedRef,
            ObjectRef? LockedRef,
            ObjectRef[] AutoWidthRefs,
            ObjectRef[] HiddenRefs);
    }
}

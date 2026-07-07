using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Main editor state and commands shared by the window and canvas.</summary>
    /// <param name="sprites">Already-preloaded sprite cache for the active content.</param>
    /// <param name="settings">Persisted editor settings store, or null in tests that don't exercise persistence.</param>
    /// <param name="initial">The editor settings snapshot loaded at startup (decoration defaults, content path).</param>
    public sealed partial class EditorViewModel(SpriteCache sprites, ISettingsStore? settings = null, EditorSettings? initial = null) : ViewModelBase
    {
        private readonly DescriptorTable _descriptors = DescriptorTable.Default;
        private readonly List<HistoryState> _undoStack = [];
        private readonly List<HistoryState> _redoStack = [];
        private HistoryState? _pendingUndoTransaction;

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
        [ObservableProperty] public partial bool ShowMobileHitboxes { get; set; }
        [ObservableProperty] public partial int ActiveRopeSkin { get; set; }
        [ObservableProperty] public partial int ActiveBackground { get; set; }
        [ObservableProperty] public partial int ActiveCandySkin { get; set; }
        [ObservableProperty] public partial int ActiveOmNomSupport { get; set; }

        /// <summary>Sprite cache for the active content.</summary>
        public SpriteCache Sprites { get; } = sprites;

        /// <summary>Palette items available for placement.</summary>
        public ObservableCollection<PaletteItemViewModel> Palette { get; } = [];

        /// <summary>Attribute fields for the selected object.</summary>
        public ObservableCollection<AttributeFieldViewModel> Fields { get; } = [];

        /// <summary>Objects in the current level, mirrored for list binding.</summary>
        public ObservableCollection<LevelObject> ObjectList { get; } = [];

        /// <summary>Raised when a selected object's editable values change.</summary>
        public event Action? ObjectMutated;

        /// <summary>Raised after a level XML document has loaded into the editor.</summary>
        public event Action? LevelLoaded;

        /// <summary>The current level's editable settings, or null when no level is loaded.</summary>
        public LevelSettings? CurrentSettings => Document?.Settings;

        /// <summary>True when a level is open and editor-only commands can run.</summary>
        public bool HasDocument => Document is not null;

        /// <summary>True when an undo snapshot is available.</summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>True when a redo snapshot is available.</summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>Loads a level from its XML text into the editor.</summary>
        public void LoadLevelXml(string xml)
        {
            Document = LevelDocument.Parse(xml);
            SelectedObject = null;
            LockedObject = null;
            ClearHistory();
            // The canvas fits the level to the viewport once it is laid out (LevelCanvas.FitToView).
            RefreshPalette();
            RefreshObjectList();
            LevelLoaded?.Invoke();
        }

        /// <summary>Closes the current level and clears document-scoped editor state.</summary>
        public void CloseLevel()
        {
            Document = null;
            SelectedObject = null;
            LockedObject = null;
            ClearHistory();
            Palette.Clear();
            ObjectList.Clear();
            Fields.Clear();
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
            LevelLoaded?.Invoke();
        }

        /// <summary>Writes edited settings back into the current level and refreshes the view.</summary>
        public void UpdateLevelSettings(LevelSettings settings)
        {
            if (Document is null)
            {
                return;
            }
            CaptureUndoSnapshot();
            Document.UpdateSettings(settings);
            RefreshPalette();
            RefreshObjectList();
            if (SelectedObject is not null && !Document.Objects.Contains(SelectedObject))
            {
                SelectedObject = null;
            }
            if (LockedObject is not null && !Document.Objects.Contains(LockedObject))
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
            if (SelectedObject is null || Document is null)
            {
                return;
            }

            LevelObject removed = SelectedObject;
            CaptureUndoSnapshot();
            LevelDocument.Remove(removed);
            if (Equals(LockedObject, removed))
            {
                LockedObject = null;
            }
            SelectedObject = null;
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

        /// <summary>Refreshes the object list from the current document.</summary>
        public void RefreshObjectList()
        {
            ObjectList.Clear();
            if (Document is null)
            {
                return;
            }
            foreach (LevelObject obj in Document.Objects)
            {
                ObjectList.Add(obj);
            }
        }

        /// <summary>Refreshes palette availability from descriptor cardinality and loaded objects.</summary>
        public void RefreshPalette()
        {
            IReadOnlyList<LevelObject> objs = Document?.Objects ?? [];
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
                    Sprites.GetThumbnail(d.ElementName, ActiveCandySkin, ActiveOmNomSupport)));
            }
        }

        // Candy type follows twoParts. When no document is
        // loaded, everything is shown (disabled) so the palette isn't empty on startup.
        private static bool IsAvailableInLevel(string element, LevelDocument doc)
        {
            return element switch
            {
                "candy" => !doc.TwoParts,
                "candyL" or "candyR" => doc.TwoParts,
                _ => true,
            };
        }

        /// <summary>Places a new object if the descriptor exists and capacity allows it.</summary>
        public LevelObject? PlaceObject(string element, int levelX, int levelY)
        {
            ObjectDescriptor? d = _descriptors.For(element);
            if (d is null || Document is null || LockedObject is not null || Cardinality.IsAtCapacity(d, Document.Objects))
            {
                return null;
            }

            CaptureUndoSnapshot();
            LevelObject obj = Placement.CreateObject(d, levelX, levelY);
            LevelObjectPolicy.ApplyDefaults(obj, Document);
            Document.Add(obj);
            RefreshPalette();
            RefreshObjectList();
            SelectedObject = obj;
            return obj;
        }

        /// <summary>Serializes the current level to XML text, or null when no document is loaded.</summary>
        public string? ToXml()
        {
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
            if (before.Xml == Document.Save())
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
            foreach (AttributeFieldViewModel field in Fields)
            {
                field.Refresh();
            }
        }

        partial void OnSelectedObjectChanged(LevelObject? value)
        {
            PopulateFields(value);
        }

        partial void OnDocumentChanged(LevelDocument? value)
        {
            OnPropertyChanged(nameof(HasDocument));
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
                ObjectMutated?.Invoke();
            }

            void Changing()
            {
                CaptureUndoSnapshot();
            }

            Fields.Add(new AttributeFieldViewModel(value, "x", AttrType.Whole, null, Changed, Changing));
            Fields.Add(new AttributeFieldViewModel(value, "y", AttrType.Whole, null, Changed, Changing));

            if (value.Type == "grab" && Document is not null)
            {
                GrabFieldBuilder.Build(Fields, value, Document, Changed, Changing, () => PopulateFields(value));
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
                    Fields.Add(new AttributeFieldViewModel(value, spec.Name, spec.Type, spec.EnumValues, Changed, Changing));
                }
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
            if (_undoStack.Count > 0 && _undoStack[^1].Xml == state.Xml)
            {
                return;
            }

            _undoStack.Add(state);
            _redoStack.Clear();
            NotifyHistoryChanged();
        }

        private HistoryState? CreateHistoryState()
        {
            return Document is null
                ? null
                : new HistoryState(Document.Save(), IndexOf(Document.Objects, SelectedObject), IndexOf(Document.Objects, LockedObject));
        }

        private void RestoreHistoryState(HistoryState state)
        {
            Document = LevelDocument.Parse(state.Xml);
            RefreshPalette();
            RefreshObjectList();
            SelectedObject = ObjectAt(state.SelectedIndex);
            LockedObject = ObjectAt(state.LockedIndex);
            LevelLoaded?.Invoke();
            ObjectMutated?.Invoke();
        }

        private LevelObject? ObjectAt(int index)
        {
            return Document is { } doc && index >= 0 && index < doc.Objects.Count
                ? doc.Objects[index]
                : null;
        }

        private static int IndexOf(IReadOnlyList<LevelObject> objects, LevelObject? target)
        {
            if (target is null)
            {
                return -1;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (Equals(objects[i], target))
                {
                    return i;
                }
            }
            return -1;
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

        private sealed record HistoryState(string Xml, int SelectedIndex, int LockedIndex);
    }
}

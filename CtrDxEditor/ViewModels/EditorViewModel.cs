using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    public sealed partial class EditorViewModel : ViewModelBase
    {
        private readonly string _contentRoot;
        private readonly DescriptorTable _descriptors = DescriptorTable.Default;

        [ObservableProperty] private LevelDocument? _document;
        [ObservableProperty] private ViewTransform _view = ViewTransform.Identity;
        [ObservableProperty] private LevelObject? _selectedObject;
        [ObservableProperty] private LevelObject? _lockedObject;
        [ObservableProperty] private bool _snapEnabled;
        [ObservableProperty] private bool _showHitboxes = true;
        [ObservableProperty] private bool _showMobileHitboxes;

        public SpriteCache Sprites { get; }
        public ObservableCollection<PaletteItemViewModel> Palette { get; } = [];
        public ObservableCollection<AttributeFieldViewModel> Fields { get; } = [];
        public ObservableCollection<LevelObject> ObjectList { get; } = [];
        public event Action? ObjectMutated;

        public EditorViewModel(string contentRoot)
        {
            _contentRoot = contentRoot;
            Sprites = new SpriteCache(_contentRoot);
        }

        public void LoadLevel(string path)
        {
            Document = LevelDocument.Load(path);
            SelectedObject = null;
            LockedObject = null;
            // The canvas fits the level to the viewport once it is laid out (LevelCanvas.FitToView).
            RefreshPalette();
            RefreshObjectList();
        }

        public void DeleteSelected()
        {
            if (SelectedObject is null || Document is null)
            {
                return;
            }

            LevelObject removed = SelectedObject;
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

        public void RefreshPalette()
        {
            IReadOnlyList<LevelObject> objs = Document?.Objects ?? [];
            Palette.Clear();
            foreach (ObjectDescriptor d in _descriptors.ByElement.Values)
            {
                bool enabled = Document is not null && !Cardinality.IsAtCapacity(d, objs);
                Palette.Add(new PaletteItemViewModel(
                    d.ElementName, Localizer.ObjectName(d.ElementName), enabled, Sprites.GetThumbnail(d.ElementName)));
            }
        }

        public LevelObject? PlaceObject(string element, int levelX, int levelY)
        {
            ObjectDescriptor? d = _descriptors.For(element);
            if (d is null || Document is null || Cardinality.IsAtCapacity(d, Document.Objects))
            {
                return null;
            }

            LevelObject obj = Placement.CreateObject(d, levelX, levelY);
            Document.Add(obj);
            RefreshPalette();
            RefreshObjectList();
            SelectedObject = obj;
            return obj;
        }

        public void SaveTo(string path)
        {
            if (Document is not null)
            {
                File.WriteAllText(path, Document.Save());
            }
        }

        partial void OnSelectedObjectChanged(LevelObject? value)
        {
            Fields.Clear();
            if (value is null)
            {
                return;
            }

            void Changed() => ObjectMutated?.Invoke();

            Fields.Add(new AttributeFieldViewModel(value, "x", null, Changed));
            Fields.Add(new AttributeFieldViewModel(value, "y", null, Changed));

            ObjectDescriptor? d = _descriptors.For(value.Type);
            if (d is not null)
            {
                foreach (AttributeSpec spec in d.Attributes)
                {
                    Fields.Add(new AttributeFieldViewModel(value, spec.Name, spec.EnumValues, Changed));
                }
            }
        }
    }
}

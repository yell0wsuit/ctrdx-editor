using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// The editor's object selection: an ordered set of objects that always live in one layer, plus the
    /// last-affected "primary" that drives specialized handles and the property panel. Objects only — layers
    /// are never part of the selection.
    /// </summary>
    public sealed class EditorSelection(LevelDocument document)
    {
        private readonly LevelDocument _document = document;
        private readonly List<LevelObject> _items = [];

        /// <summary>All selected objects, in the order they were added.</summary>
        public IReadOnlyList<LevelObject> Items => _items;

        /// <summary>The last object added or replaced; drives handles and the property panel.</summary>
        public LevelObject? Primary { get; private set; }

        /// <summary>Number of selected objects.</summary>
        public int Count => _items.Count;

        /// <summary>The primary object's layer, or null when the selection is empty. A selection may span layers.</summary>
        public LevelLayer? Layer =>
            Primary is { } p
                ? _document.Layers.FirstOrDefault(l => ReferenceEquals(l.Element, p.Element.Parent))
                : null;

        /// <summary>Raised after any mutation so consumers (canvas, property panel) can re-read the selection.</summary>
        public event Action? Changed;

        /// <summary>Clears the selection and selects a single object as the primary.</summary>
        public void Replace(LevelObject obj)
        {
            _items.Clear();
            _items.Add(obj);
            Primary = obj;
            Changed?.Invoke();
        }

        /// <summary>
        /// Adds an object to the selection, or removes it when already selected. The added object becomes the
        /// primary. A selection may span layers (Ctrl/Cmd+click on the canvas or object panel).
        /// </summary>
        public void Toggle(LevelObject obj)
        {
            int index = _items.FindIndex(o => o.Equals(obj));
            if (index >= 0)
            {
                _items.RemoveAt(index);
                if (Primary is { } p && p.Equals(obj))
                {
                    Primary = _items.Count > 0 ? _items[^1] : null;
                }
                Changed?.Invoke();
                return;
            }

            _items.Add(obj);
            Primary = obj;
            Changed?.Invoke();
        }

        /// <summary>Replaces the selection with an explicit set and primary (used by Ctrl+A).</summary>
        public void SetRange(IEnumerable<LevelObject> objs, LevelObject primary)
        {
            _items.Clear();
            _items.AddRange(objs);
            Primary = primary;
            Changed?.Invoke();
        }

        /// <summary>Empties the selection.</summary>
        public void Clear()
        {
            if (_items.Count == 0 && Primary is null)
            {
                return;
            }
            _items.Clear();
            Primary = null;
            Changed?.Invoke();
        }
    }
}

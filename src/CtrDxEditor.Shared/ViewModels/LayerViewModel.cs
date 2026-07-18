using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// A layer row in the object panel: its name, visibility, active state, and object children.
    /// </summary>
    /// <param name="layer">The document layer represented by this row.</param>
    public sealed partial class LayerViewModel(LevelLayer layer) : ObservableObject
    {
        /// <summary>The underlying document layer.</summary>
        public LevelLayer Layer { get; } = layer;

        /// <summary>The layer's display name.</summary>
        [ObservableProperty] public partial string Name { get; set; } = layer.Name;

        /// <summary>Whether the layer and its objects are shown on the canvas. Session-only.</summary>
        [ObservableProperty] public partial bool IsVisible { get; set; } = true;

        /// <summary>Whether this layer receives newly placed objects.</summary>
        [ObservableProperty] public partial bool IsActive { get; set; }

        /// <summary>Whether editing of this layer's objects is blocked. Session-only.</summary>
        [ObservableProperty] public partial bool IsLocked { get; set; }

        /// <summary>Whether this layer's object children are expanded in the tree. Session-only.</summary>
        [ObservableProperty] public partial bool IsExpanded { get; set; } = true;

        /// <summary>Whether the layer name is currently being edited inline. Session-only.</summary>
        [ObservableProperty] public partial bool IsRenaming { get; set; }

        /// <summary>The layer's object rows, in XML order.</summary>
        public ObservableCollection<LevelObject> Objects { get; } = [];
    }
}

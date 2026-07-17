using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// A run of property panel fields shown together. A null <see cref="Header"/> means the fields render
    /// bare, exactly as an ungrouped panel always has; a non-null header renders them inside a collapsible
    /// expander. Used by the mechanical hand to give each segment its own section.
    /// </summary>
    /// <param name="header">The section title, or null to render the fields bare.</param>
    /// <param name="index">A stable identity for the group, or -1 for the anonymous ungrouped section.</param>
    public sealed partial class PropertyGroupViewModel(string? header, int index) : ViewModelBase
    {
        /// <summary>The section title, or null when the fields render bare.</summary>
        public string? Header { get; } = header;

        /// <summary>A stable identity for the group; -1 for the anonymous ungrouped section.</summary>
        public int Index { get; } = index;

        /// <summary>Whether this group renders as a collapsible expander.</summary>
        public bool HasHeader => Header is not null;

        /// <summary>Whether the expander is open.</summary>
        [ObservableProperty] public partial bool IsExpanded { get; set; } = true;

        /// <summary>The fields in this section, in panel order.</summary>
        public ObservableCollection<AttributeFieldViewModel> Fields { get; } = [];
    }
}

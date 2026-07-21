using System.Collections.Generic;
using System.Globalization;
using System.Text;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Backs the Review Changes dialog. Built once from a baseline/live XML pair and never updated - the
    /// editor is inert behind the modal dialog, so there is nothing to observe.
    /// </summary>
    public sealed partial class ReviewChangesViewModel : ViewModelBase
    {
        private static readonly CompositeFormat SummaryFormat =
            CompositeFormat.Parse(Localizer.Get("Dialog.Review.Summary"));

        /// <summary>Diffs the saved baseline against the live document and prepares both view modes.</summary>
        public ReviewChangesViewModel(string oldXml, string newXml)
        {
            LevelDiffResult result = LevelDiff.Build(oldXml, newXml);
            Rows = result.Rows;
            UnifiedRows = LevelDiff.ToUnified(result.Rows);
            HasChanges = result.HasChanges;
            Summary = string.Format(
                CultureInfo.CurrentCulture,
                SummaryFormat,
                result.Added,
                result.Removed,
                result.Modified);
        }

        /// <summary>Paired baseline/live rows, rendered by the split view.</summary>
        public IReadOnlyList<DiffRow> Rows { get; }

        /// <summary>The same diff flattened into one column, rendered by the unified view.</summary>
        public IReadOnlyList<UnifiedDiffRow> UnifiedRows { get; }

        /// <summary>Whether the level differs from its baseline; false shows the empty state instead of the list.</summary>
        public bool HasChanges { get; }

        /// <summary>Change tallies shown under the dialog title.</summary>
        public string Summary { get; }

        /// <summary>Whether the side-by-side view is showing; the dialog opens split by default.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsUnifiedView))]
        public partial bool IsSplitView { get; set; } = true;

        /// <summary>Inverse of <see cref="IsSplitView"/>, so the unified pane can bind its visibility directly.</summary>
        public bool IsUnifiedView => !IsSplitView;
    }
}

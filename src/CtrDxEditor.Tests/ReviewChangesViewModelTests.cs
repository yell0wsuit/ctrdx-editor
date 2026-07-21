using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Verifies the Review Changes dialog's view model: its default view mode, the empty-state flag, and
    /// the summary line, all without constructing any UI.
    /// </summary>
    public class ReviewChangesViewModelTests
    {
        private const string Base = "<map>\n  <a x=\"1\"/>\n  <b y=\"2\"/>\n</map>";

        /// <summary>The dialog opens side-by-side, per the design's default.</summary>
        [Fact]
        public void DefaultsToSplitView()
        {
            ReviewChangesViewModel vm = new(Base, Base);

            Assert.True(vm.IsSplitView);
            Assert.False(vm.IsUnifiedView);
        }

        /// <summary>Toggling the view mode flips the inverse flag the unified template binds to.</summary>
        [Fact]
        public void UnifiedFlagTracksSplitFlag()
        {
            ReviewChangesViewModel vm = new(Base, Base) { IsSplitView = false };

            Assert.True(vm.IsUnifiedView);
        }

        /// <summary>An unmodified level reports no changes, which drives the empty state.</summary>
        [Fact]
        public void IdenticalInputHasNoChanges()
        {
            ReviewChangesViewModel vm = new(Base, Base);

            Assert.False(vm.HasChanges);
            Assert.NotEmpty(vm.Rows);
        }

        /// <summary>The summary line reports the tallies in added/removed/modified order.</summary>
        [Fact]
        public void SummaryReportsCounts()
        {
            // An added line, not a replaced one: DiffPlex reports a swapped line as Modified, so this
            // fixture inserts <c> ahead of the untouched <b> to produce a genuine insertion.
            string modified = "<map>\n  <a x=\"1\"/>\n  <c z=\"3\"/>\n  <b y=\"2\"/>\n</map>";

            ReviewChangesViewModel vm = new(Base, modified);

            Assert.True(vm.HasChanges);
            Assert.Equal("1 added · 0 removed · 0 modified", vm.Summary);
            Assert.NotEmpty(vm.UnifiedRows);
        }
    }
}

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Verifies the DiffPlex projection used by the Review Changes dialog: rows pair the two padded
    /// panes, line numbers stay correct across padding, and the counts match the emitted rows.
    /// </summary>
    public class LevelDiffTests
    {
        private const string Base = "<map>\n  <a x=\"1\"/>\n  <b y=\"2\"/>\n</map>";

        /// <summary>Identical input produces only unchanged rows and reports no changes.</summary>
        [Fact]
        public void IdenticalInputHasNoChanges()
        {
            LevelDiffResult result = LevelDiff.Build(Base, Base);

            Assert.False(result.HasChanges);
            Assert.Equal(0, result.Added);
            Assert.Equal(0, result.Removed);
            Assert.Equal(0, result.Modified);
            Assert.All(result.Rows, row => Assert.Equal(DiffRowKind.Unchanged, row.Kind));
        }

        /// <summary>An unchanged row carries the same text and a line number on both sides.</summary>
        [Fact]
        public void UnchangedRowCarriesBothSides()
        {
            LevelDiffResult result = LevelDiff.Build(Base, Base);

            DiffRow first = result.Rows[0];
            Assert.Equal("<map>", first.OldText);
            Assert.Equal("<map>", first.NewText);
            Assert.Equal(1, first.OldLine);
            Assert.Equal(1, first.NewLine);
        }

        /// <summary>An inserted line has no old side and counts as added.</summary>
        [Fact]
        public void InsertedLineHasNoOldSide()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <c z=\"3\"/>\n  <b y=\"2\"/>\n</map>";

            LevelDiffResult result = LevelDiff.Build(Base, modified);

            DiffRow added = Assert.Single(result.Rows, row => row.Kind == DiffRowKind.Inserted);
            Assert.Null(added.OldLine);
            Assert.Null(added.OldText);
            Assert.Equal("  <c z=\"3\"/>", added.NewText);
            Assert.Equal(1, result.Added);
            Assert.True(result.HasChanges);
        }

        /// <summary>A deleted line has no new side and counts as removed.</summary>
        [Fact]
        public void DeletedLineHasNoNewSide()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n</map>";

            LevelDiffResult result = LevelDiff.Build(Base, modified);

            DiffRow removed = Assert.Single(result.Rows, row => row.Kind == DiffRowKind.Deleted);
            Assert.Null(removed.NewLine);
            Assert.Null(removed.NewText);
            Assert.Equal("  <b y=\"2\"/>", removed.OldText);
            Assert.Equal(1, result.Removed);
        }

        /// <summary>An edited line pairs both sides on one row and counts as modified, not add+remove.</summary>
        [Fact]
        public void ModifiedLinePairsBothSides()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";

            LevelDiffResult result = LevelDiff.Build(Base, modified);

            DiffRow row = Assert.Single(result.Rows, r => r.Kind == DiffRowKind.Modified);
            Assert.Equal("  <b y=\"2\"/>", row.OldText);
            Assert.Equal("  <b y=\"9\"/>", row.NewText);
            Assert.Equal(1, result.Modified);
            Assert.Equal(0, result.Added);
            Assert.Equal(0, result.Removed);
        }

        /// <summary>Line numbers keep counting correctly on both sides after an insertion shifts them.</summary>
        [Fact]
        public void LineNumbersSurvivePadding()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <c z=\"3\"/>\n  <b y=\"2\"/>\n</map>";

            LevelDiffResult result = LevelDiff.Build(Base, modified);

            DiffRow lastRow = result.Rows[^1];
            Assert.Equal("</map>", lastRow.OldText);
            Assert.Equal(4, lastRow.OldLine);
            Assert.Equal(5, lastRow.NewLine);
        }

        /// <summary>The kind booleans the dialog binds to agree with the row kind.</summary>
        [Fact]
        public void KindBooleansMatchKind()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";

            DiffRow row = Assert.Single(LevelDiff.Build(Base, modified).Rows, r => r.IsModified);

            Assert.Equal(DiffRowKind.Modified, row.Kind);
            Assert.False(row.IsAdded);
            Assert.False(row.IsRemoved);
            Assert.False(row.IsUnchanged);
        }
    }
}

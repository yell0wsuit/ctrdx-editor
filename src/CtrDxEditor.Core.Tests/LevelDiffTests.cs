using System.Collections.Generic;
using System.Linq;

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

        /// <summary>A modified line becomes two unified rows: the old text, then the new text.</summary>
        [Fact]
        public void UnifiedSplitsModifiedIntoTwoRows()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";
            LevelDiffResult result = LevelDiff.Build(Base, modified);

            IReadOnlyList<UnifiedDiffRow> unified = LevelDiff.ToUnified(result.Rows);

            UnifiedDiffRow oldSide = unified[2];
            UnifiedDiffRow newSide = unified[3];
            Assert.Equal(DiffRowKind.Deleted, oldSide.Kind);
            Assert.Equal("  <b y=\"2\"/>", oldSide.Text);
            Assert.Null(oldSide.NewLine);
            Assert.Equal(DiffRowKind.Inserted, newSide.Kind);
            Assert.Equal("  <b y=\"9\"/>", newSide.Text);
            Assert.Null(newSide.OldLine);
        }

        /// <summary>Unchanged lines pass through as a single context row keeping both line numbers.</summary>
        [Fact]
        public void UnifiedKeepsUnchangedAsOneRow()
        {
            IReadOnlyList<UnifiedDiffRow> unified = LevelDiff.ToUnified(LevelDiff.Build(Base, Base).Rows);

            Assert.Equal(4, unified.Count);
            Assert.All(unified, row => Assert.Equal(DiffRowKind.Unchanged, row.Kind));
            Assert.Equal(1, unified[0].OldLine);
            Assert.Equal(1, unified[0].NewLine);
        }

        /// <summary>Each unified row carries a text marker, so the diff reads without relying on color.</summary>
        [Fact]
        public void UnifiedRowsCarryMarkers()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <c z=\"3\"/>\n  <b y=\"2\"/>\n</map>";

            IReadOnlyList<UnifiedDiffRow> unified = LevelDiff.ToUnified(LevelDiff.Build(Base, modified).Rows);

            Assert.Equal("+", Assert.Single(unified, row => row.IsAdded).Marker);
            Assert.All(unified, row => Assert.False(string.IsNullOrEmpty(row.Marker)));
            Assert.Contains(unified, row => row.Marker == " ");
        }

        /// <summary>An unchanged line is one run covering the whole text, so it renders with no highlight.</summary>
        [Fact]
        public void UnchangedRowIsASingleUnchangedRun()
        {
            DiffRow row = LevelDiff.Build(Base, Base).Rows[0];

            DiffRun run = Assert.Single(row.NewRuns);
            Assert.Equal("<map>", run.Text);
            Assert.False(run.IsChanged);
        }

        /// <summary>A modified line marks only the edited characters, leaving the shared text unhighlighted.</summary>
        [Fact]
        public void ModifiedRowMarksOnlyTheEditedCharacters()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";

            DiffRow row = Assert.Single(LevelDiff.Build(Base, modified).Rows, r => r.IsModified);

            DiffRun oldChanged = Assert.Single(row.OldRuns, r => r.IsChanged);
            DiffRun newChanged = Assert.Single(row.NewRuns, r => r.IsChanged);
            Assert.Equal("2", oldChanged.Text);
            Assert.Equal("9", newChanged.Text);
        }

        /// <summary>Runs concatenate back to the line, so nothing is dropped or duplicated in rendering.</summary>
        [Fact]
        public void RunsConcatenateBackToTheLineText()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";

            DiffRow row = Assert.Single(LevelDiff.Build(Base, modified).Rows, r => r.IsModified);

            Assert.Equal(row.OldText, string.Concat(row.OldRuns.Select(r => r.Text)));
            Assert.Equal(row.NewText, string.Concat(row.NewRuns.Select(r => r.Text)));
        }

        /// <summary>Neighbouring changed characters merge into one run, so a run of edits reads as one block.</summary>
        [Fact]
        public void AdjacentChangedCharactersCoalesceIntoOneRun()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"987\"/>\n</map>";

            DiffRow row = Assert.Single(LevelDiff.Build(Base, modified).Rows, r => r.IsModified);

            DiffRun changed = Assert.Single(row.NewRuns, r => r.IsChanged);
            Assert.Equal("987", changed.Text);
        }

        /// <summary>A one-sided line has no counterpart to compare against, so it is one changed run.</summary>
        [Fact]
        public void InsertedRowIsASingleChangedRun()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <c z=\"3\"/>\n  <b y=\"2\"/>\n</map>";

            DiffRow row = Assert.Single(LevelDiff.Build(Base, modified).Rows, r => r.IsAdded);

            DiffRun run = Assert.Single(row.NewRuns);
            Assert.Equal("  <c z=\"3\"/>", run.Text);
            Assert.True(run.IsChanged);
            Assert.Empty(row.OldRuns);
        }

        /// <summary>Unified rows carry the runs of whichever side they came from.</summary>
        [Fact]
        public void UnifiedRowsCarryTheirSideRuns()
        {
            string modified = "<map>\n  <a x=\"1\"/>\n  <b y=\"9\"/>\n</map>";
            LevelDiffResult result = LevelDiff.Build(Base, modified);

            IReadOnlyList<UnifiedDiffRow> unified = LevelDiff.ToUnified(result.Rows);

            Assert.Equal("2", Assert.Single(unified[2].Runs, r => r.IsChanged).Text);
            Assert.Equal("9", Assert.Single(unified[3].Runs, r => r.IsChanged).Text);
        }
    }
}

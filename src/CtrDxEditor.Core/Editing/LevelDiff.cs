using System.Collections.Generic;
using System.Text;

using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>What happened to one line between the saved baseline and the live document.</summary>
    public enum DiffRowKind
    {
        /// <summary>Present and identical on both sides.</summary>
        Unchanged,

        /// <summary>Only in the live document.</summary>
        Inserted,

        /// <summary>Only in the saved baseline.</summary>
        Deleted,

        /// <summary>Present on both sides with different text.</summary>
        Modified,
    }

    /// <summary>
    /// A stretch of one line that is either wholly edited or wholly shared with the other side. Rendering
    /// these in sequence gives the intra-line highlight: the changed characters sit on a stronger tint
    /// than the rest of the row.
    /// </summary>
    public sealed record DiffRun(string Text, bool IsChanged);

    /// <summary>
    /// One row of the diff, pairing the baseline and live sides. The side that does not exist for this
    /// row (the deleted side of an insertion, and vice versa) has a null line number and null text.
    /// </summary>
    public sealed record DiffRow(
        int? OldLine,
        string? OldText,
        int? NewLine,
        string? NewText,
        DiffRowKind Kind,
        IReadOnlyList<DiffRun> OldRuns,
        IReadOnlyList<DiffRun> NewRuns)
    {
        /// <summary>Whether this line exists only in the live document; drives the added row style.</summary>
        public bool IsAdded => Kind == DiffRowKind.Inserted;

        /// <summary>Whether this line exists only in the baseline; drives the removed row style.</summary>
        public bool IsRemoved => Kind == DiffRowKind.Deleted;

        /// <summary>Whether this line differs between the two sides; drives the modified row style.</summary>
        public bool IsModified => Kind == DiffRowKind.Modified;

        /// <summary>Whether this line is identical on both sides; such rows render without a highlight.</summary>
        public bool IsUnchanged => Kind == DiffRowKind.Unchanged;
    }

    /// <summary>
    /// One row of the unified (single-column) view. A modified line expands into two of these - the
    /// baseline text as a deletion, then the live text as an insertion - matching how unified diffs read.
    /// </summary>
    public sealed record UnifiedDiffRow(
        int? OldLine,
        int? NewLine,
        string Text,
        DiffRowKind Kind,
        IReadOnlyList<DiffRun> Runs)
    {
        /// <summary>Gutter marker, so the row's kind is readable without relying on its color.</summary>
        public string Marker => Kind switch
        {
            DiffRowKind.Inserted => "+",
            DiffRowKind.Deleted => "-",
            DiffRowKind.Modified => "~",
            DiffRowKind.Unchanged => " ",
            _ => " ",
        };

        /// <summary>Whether this line exists only in the live document; drives the added row style.</summary>
        public bool IsAdded => Kind == DiffRowKind.Inserted;

        /// <summary>Whether this line exists only in the baseline; drives the removed row style.</summary>
        public bool IsRemoved => Kind == DiffRowKind.Deleted;

        /// <summary>Whether this line differs between the two sides; drives the modified row style.</summary>
        public bool IsModified => Kind == DiffRowKind.Modified;

        /// <summary>Whether this line is identical on both sides; such rows render without a highlight.</summary>
        public bool IsUnchanged => Kind == DiffRowKind.Unchanged;
    }

    /// <summary>The full line-by-line comparison of two level XML documents, plus its change tallies.</summary>
    public sealed record LevelDiffResult(
        IReadOnlyList<DiffRow> Rows,
        int Added,
        int Removed,
        int Modified)
    {
        /// <summary>Whether the two documents differ at all; false drives the dialog's empty state.</summary>
        public bool HasChanges => Added > 0 || Removed > 0 || Modified > 0;
    }

    /// <summary>
    /// Compares two level XML documents line by line for the Review Changes dialog. This is presentation
    /// input only - it never mutates a document, and the editor's own undo stack remains the way to revert.
    /// </summary>
    public static class LevelDiff
    {
        // Level XML is dense with numeric attributes, where an edit is usually a digit or two inside an
        // otherwise identical line. Chunking sub-pieces by character rather than by word narrows the
        // highlight to exactly those digits; adjacent ones are coalesced back into one run below.
        private static readonly SideBySideDiffBuilder Builder =
            new(Differ.Instance, new LineChunker(), new CharacterChunker());

        private static readonly IReadOnlyList<DiffRun> NoRuns = [];

        /// <summary>
        /// Builds the line-by-line comparison of <paramref name="oldXml"/> against <paramref name="newXml"/>.
        /// DiffPlex pads both panes to equal length with imaginary pieces, so the two sides are zipped by
        /// index into one row list that both the split and unified views render.
        /// </summary>
        public static LevelDiffResult Build(string oldXml, string newXml)
        {
            SideBySideDiffModel model = Builder.BuildDiffModel(oldXml, newXml);
            List<DiffRow> rows = [with(model.OldText.Lines.Count)];
            int added = 0;
            int removed = 0;
            int modified = 0;

            for (int i = 0; i < model.OldText.Lines.Count; i++)
            {
                DiffPiece oldPiece = model.OldText.Lines[i];
                DiffPiece newPiece = model.NewText.Lines[i];
                DiffRowKind kind = Classify(oldPiece.Type, newPiece.Type);

                switch (kind)
                {
                    case DiffRowKind.Inserted:
                        added++;
                        break;
                    case DiffRowKind.Deleted:
                        removed++;
                        break;
                    case DiffRowKind.Modified:
                        modified++;
                        break;
                    case DiffRowKind.Unchanged:
                        break;
                    default:
                        break;
                }

                // Imaginary pieces are padding: DiffPlex leaves both Position and Text null on them,
                // so the absent side stays null rather than rendering as an empty numbered line.
                rows.Add(new DiffRow(
                    oldPiece.Type == ChangeType.Imaginary ? null : oldPiece.Position,
                    oldPiece.Type == ChangeType.Imaginary ? null : oldPiece.Text,
                    newPiece.Type == ChangeType.Imaginary ? null : newPiece.Position,
                    newPiece.Type == ChangeType.Imaginary ? null : newPiece.Text,
                    kind,
                    BuildRuns(oldPiece),
                    BuildRuns(newPiece)));
            }

            return new LevelDiffResult(rows, added, removed, modified);
        }

        /// <summary>
        /// Flattens paired rows into the single-column unified sequence. Unchanged and single-sided rows
        /// pass through as one row each; a modified row expands into its deletion followed by its insertion.
        /// </summary>
        public static IReadOnlyList<UnifiedDiffRow> ToUnified(IReadOnlyList<DiffRow> rows)
        {
            List<UnifiedDiffRow> unified = [with(rows.Count)];

            foreach (DiffRow row in rows)
            {
                switch (row.Kind)
                {
                    case DiffRowKind.Modified:
                        unified.Add(new UnifiedDiffRow(row.OldLine, null, row.OldText ?? string.Empty, DiffRowKind.Deleted, row.OldRuns));
                        unified.Add(new UnifiedDiffRow(null, row.NewLine, row.NewText ?? string.Empty, DiffRowKind.Inserted, row.NewRuns));
                        break;
                    case DiffRowKind.Deleted:
                        unified.Add(new UnifiedDiffRow(row.OldLine, null, row.OldText ?? string.Empty, DiffRowKind.Deleted, row.OldRuns));
                        break;
                    case DiffRowKind.Inserted:
                        unified.Add(new UnifiedDiffRow(null, row.NewLine, row.NewText ?? string.Empty, DiffRowKind.Inserted, row.NewRuns));
                        break;
                    case DiffRowKind.Unchanged:
                    default:
                        unified.Add(new UnifiedDiffRow(row.OldLine, row.NewLine, row.NewText ?? row.OldText ?? string.Empty, DiffRowKind.Unchanged, row.NewRuns.Count > 0 ? row.NewRuns : row.OldRuns));
                        break;
                }
            }

            return unified;
        }

        /// <summary>
        /// Projects one line's sub-piece diff into renderable runs. DiffPlex only fills SubPieces for a
        /// line it matched against a counterpart, so a wholly inserted or deleted line falls back to one
        /// changed run covering it, and an unchanged line to one unchanged run.
        /// </summary>
        private static IReadOnlyList<DiffRun> BuildRuns(DiffPiece piece)
        {
            if (piece.Type == ChangeType.Imaginary)
            {
                return NoRuns;
            }

            if (piece.SubPieces.Count == 0)
            {
                string text = piece.Text ?? string.Empty;
                return text.Length == 0 ? NoRuns : [new DiffRun(text, piece.Type != ChangeType.Unchanged)];
            }

            // Character chunking emits one sub-piece per character, so runs of the same kind are merged
            // into a single block - otherwise a three-digit edit would render as three separate highlights.
            List<DiffRun> runs = [];
            StringBuilder pending = new();
            bool pendingChanged = false;

            foreach (DiffPiece sub in piece.SubPieces)
            {
                if (sub.Type == ChangeType.Imaginary || sub.Text is null)
                {
                    continue;
                }

                bool changed = sub.Type != ChangeType.Unchanged;
                if (pending.Length > 0 && changed != pendingChanged)
                {
                    runs.Add(new DiffRun(pending.ToString(), pendingChanged));
                    _ = pending.Clear();
                }

                pendingChanged = changed;
                _ = pending.Append(sub.Text);
            }

            if (pending.Length > 0)
            {
                runs.Add(new DiffRun(pending.ToString(), pendingChanged));
            }

            return runs;
        }

        // A row's kind comes from whichever side is real: an imaginary new side means the line was
        // deleted, an imaginary old side means it was inserted. Both real and differing is a modification.
        private static DiffRowKind Classify(ChangeType oldType, ChangeType newType)
        {
            return (oldType, newType) switch
            {
                (ChangeType.Imaginary, _) => DiffRowKind.Inserted,
                (_, ChangeType.Imaginary) => DiffRowKind.Deleted,
                (ChangeType.Modified, _) or (_, ChangeType.Modified) => DiffRowKind.Modified,
                (ChangeType.Inserted, _) => DiffRowKind.Inserted,
                (ChangeType.Deleted, _) => DiffRowKind.Deleted,
                _ => DiffRowKind.Unchanged,
            };
        }
    }
}

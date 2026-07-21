using System.Collections.Generic;

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
    /// One row of the diff, pairing the baseline and live sides. The side that does not exist for this
    /// row (the deleted side of an insertion, and vice versa) has a null line number and null text.
    /// </summary>
    public sealed record DiffRow(
        int? OldLine,
        string? OldText,
        int? NewLine,
        string? NewText,
        DiffRowKind Kind)
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
        DiffRowKind Kind)
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
        /// <summary>
        /// Builds the line-by-line comparison of <paramref name="oldXml"/> against <paramref name="newXml"/>.
        /// DiffPlex pads both panes to equal length with imaginary pieces, so the two sides are zipped by
        /// index into one row list that both the split and unified views render.
        /// </summary>
        public static LevelDiffResult Build(string oldXml, string newXml)
        {
            SideBySideDiffModel model = SideBySideDiffBuilder.Diff(oldXml, newXml);
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
                    kind));
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
                        unified.Add(new UnifiedDiffRow(row.OldLine, null, row.OldText ?? string.Empty, DiffRowKind.Deleted));
                        unified.Add(new UnifiedDiffRow(null, row.NewLine, row.NewText ?? string.Empty, DiffRowKind.Inserted));
                        break;
                    case DiffRowKind.Deleted:
                        unified.Add(new UnifiedDiffRow(row.OldLine, null, row.OldText ?? string.Empty, DiffRowKind.Deleted));
                        break;
                    case DiffRowKind.Inserted:
                        unified.Add(new UnifiedDiffRow(null, row.NewLine, row.NewText ?? string.Empty, DiffRowKind.Inserted));
                        break;
                    case DiffRowKind.Unchanged:
                    default:
                        unified.Add(new UnifiedDiffRow(row.OldLine, row.NewLine, row.NewText ?? row.OldText ?? string.Empty, DiffRowKind.Unchanged));
                        break;
                }
            }

            return unified;
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

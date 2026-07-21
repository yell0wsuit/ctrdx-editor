using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.Controls
{
    /// <summary>
    /// Attached properties that render a diff line as styled inlines on a plain TextBlock, so the
    /// characters that actually changed sit on a stronger tint than the rest of the row. TextBlock's
    /// Inlines collection cannot be data-bound directly, so the runs arrive through
    /// <see cref="RunsProperty"/> and are rebuilt into the collection whenever they change.
    /// </summary>
    public static class DiffTextBlock
    {
        /// <summary>The line's segments, in order; changed ones get the highlight brush painted behind them.</summary>
        public static readonly AttachedProperty<IReadOnlyList<DiffRun>?> RunsProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.TextBlock, IReadOnlyList<DiffRun>?>(
                "Runs", typeof(DiffTextBlock));

        /// <summary>
        /// The background painted behind the changed segments. Set per row kind from a style, so an
        /// insertion, a deletion and a modification each highlight in their own family.
        /// </summary>
        public static readonly AttachedProperty<IBrush?> HighlightBrushProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.TextBlock, IBrush?>(
                "HighlightBrush", typeof(DiffTextBlock));

        static DiffTextBlock()
        {
            _ = RunsProperty.Changed.AddClassHandler<Avalonia.Controls.TextBlock>((tb, _) => Rebuild(tb));
            _ = HighlightBrushProperty.Changed.AddClassHandler<Avalonia.Controls.TextBlock>((tb, _) => Rebuild(tb));
        }

        /// <summary>Reads the runs attached to <paramref name="target"/>.</summary>
        public static IReadOnlyList<DiffRun>? GetRuns(Avalonia.Controls.TextBlock target)
        {
            return target.GetValue(RunsProperty);
        }

        /// <summary>Attaches the runs to render on <paramref name="target"/>.</summary>
        public static void SetRuns(Avalonia.Controls.TextBlock target, IReadOnlyList<DiffRun>? value)
        {
            _ = target.SetValue(RunsProperty, value);
        }

        /// <summary>Reads the changed-segment background attached to <paramref name="target"/>.</summary>
        public static IBrush? GetHighlightBrush(Avalonia.Controls.TextBlock target)
        {
            return target.GetValue(HighlightBrushProperty);
        }

        /// <summary>Attaches the changed-segment background to <paramref name="target"/>.</summary>
        public static void SetHighlightBrush(Avalonia.Controls.TextBlock target, IBrush? value)
        {
            _ = target.SetValue(HighlightBrushProperty, value);
        }

        // Rebuilding from scratch rather than patching keeps this correct under list virtualization,
        // where one TextBlock is recycled across many rows and must not keep the previous line's runs.
        private static void Rebuild(Avalonia.Controls.TextBlock target)
        {
            IReadOnlyList<DiffRun>? runs = target.GetValue(RunsProperty);

            if (runs is null || runs.Count == 0)
            {
                target.Inlines?.Clear();
                target.Text = null;
                return;
            }

            IBrush? highlight = target.GetValue(HighlightBrushProperty);

            // Setting Text creates an implicit inline, so it is cleared first to avoid the line
            // rendering twice - once from Text and once from the runs appended below.
            target.Text = null;
            InlineCollection inlines = target.Inlines ??= [];
            inlines.Clear();

            foreach (DiffRun run in runs)
            {
                inlines.Add(new Run(run.Text)
                {
                    Background = run.IsChanged ? highlight : null,
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws the canvas's badge plates: the rounded dark chrome carrying live values over the level art.
    /// Stateless, like the other <c>*Renderer</c> types, so its layout can be measured and tested without
    /// a render target.
    /// </summary>
    /// <remarks>
    /// Public rather than internal, matching <see cref="WaterRenderer"/>: the solution defines no
    /// <c>InternalsVisibleTo</c>, so <c>Measure</c> would be untestable otherwise.
    /// </remarks>
    public static class BadgeRenderer
    {
        /// <summary>Rounded dark plate behind every on-canvas badge, opaque enough to read over level art.</summary>
        public static readonly IBrush Plate = new SolidColorBrush(Color.FromArgb(225, 25, 29, 36));

        /// <summary>
        /// Dimmed brush for a badge's label, against the bright value. Fixed rather than themed: the plate
        /// is the same dark colour in both theme variants, so its text does not vary with the variant.
        /// </summary>
        public static readonly IBrush LabelBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA3, 0xB2));

        /// <summary>Height of a one-row badge in screen pixels. Shared so every badge reads as the same component.</summary>
        public const double Height = 26;

        /// <summary>Corner radius of a badge plate, in screen pixels.</summary>
        public const float Radius = 6;

        /// <summary>Horizontal breathing room between a badge's content and each of its edges, in screen pixels.</summary>
        public const double Padding = 13;

        /// <summary>Gap between a badge and whatever it is anchored to, in screen pixels.</summary>
        public const double Gap = 12;

        /// <summary>Height of one content row inside a plate.</summary>
        private const double RowHeight = 16;

        /// <summary>Vertical breathing room above and below the content rows, sized so one row measures <see cref="Height"/>.</summary>
        private const double VerticalPadding = (Height - RowHeight) / 2;

        /// <summary>Gap between a label and its value on the same row.</summary>
        private const double LabelValueGap = 6;

        /// <summary>Gap on each side of the separator between two inline entries.</summary>
        private const double SeparatorGap = 8;

        /// <summary>Absolute ceiling on an inline plate's width before it wraps, in screen pixels.</summary>
        private const double MaxInlineWidth = 240;

        /// <summary>Share of the canvas an inline plate may occupy before it wraps.</summary>
        private const double MaxInlineWidthFraction = 0.4;

        /// <summary>Separator drawn between two entries sharing a row.</summary>
        private const string Separator = "·";

        /// <summary>Measured text widths for one label/value pair.</summary>
        /// <param name="LabelWidth">Rendered width of the localized label.</param>
        /// <param name="ValueWidth">Rendered width of the value.</param>
        public readonly record struct EntryMetrics(double LabelWidth, double ValueWidth);

        /// <summary>A measured badge: where its plate goes and where each content row sits inside it.</summary>
        /// <param name="Plate">The plate rect in screen space, already clamped into the canvas.</param>
        /// <param name="Rows">One content rect per row, in display order.</param>
        /// <param name="Wrapped">True when entries were split one per row instead of sharing a line.</param>
        public readonly record struct Layout(Rect Plate, IReadOnlyList<Rect> Rows, bool Wrapped);

        /// <summary>
        /// Builds badge text in the shared badge type style. The size is fixed rather than scaled by zoom:
        /// a badge is chrome sitting on top of the level, not part of it.
        /// </summary>
        /// <param name="text">The label to render.</param>
        /// <param name="foreground">Brush for the text.</param>
        /// <returns>The formatted text, ready to measure or draw.</returns>
        public static FormattedText Label(string text, IBrush foreground)
        {
            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.DefaultFontFamilyName, FontStyle.Normal, FontWeight.SemiBold),
                12,
                foreground);
        }

        /// <summary>
        /// Lays out a readout badge. Entries share one row while that fits the width cap; past it they
        /// split one per row. The plate is then clamped into <paramref name="bounds"/>, flipping below
        /// the anchor when there is no room above.
        /// </summary>
        /// <remarks>
        /// Takes measured widths rather than entries so layout is pure arithmetic: the test project has no
        /// Avalonia font manager, and constructing a <see cref="FormattedText"/> there would throw.
        /// </remarks>
        /// <param name="entries">Measured widths per entry, in display order; empty yields an empty layout.</param>
        /// <param name="separatorWidth">Rendered width of the inline separator.</param>
        /// <param name="anchor">Screen point the badge sits above.</param>
        /// <param name="bounds">The canvas size the plate must stay inside.</param>
        /// <returns>The measured layout.</returns>
        public static Layout Measure(
            IReadOnlyList<EntryMetrics> entries, double separatorWidth, Point anchor, Size bounds)
        {
            if (entries.Count == 0)
            {
                return new Layout(default, [], false);
            }

            double inlineWidth = InlineContentWidth(entries, separatorWidth);
            double cap = Math.Min(MaxInlineWidth, bounds.Width * MaxInlineWidthFraction);

            // A lone entry has nothing to wrap against, so it stays inline however wide it is.
            bool wrapped = entries.Count > 1 && inlineWidth + (Padding * 2) > cap;

            double[] rowWidths = wrapped ? RowWidths(entries) : [inlineWidth];
            double contentWidth = 0;
            foreach (double width in rowWidths)
            {
                contentWidth = Math.Max(contentWidth, width);
            }

            double plateWidth = contentWidth + (Padding * 2);
            double plateHeight = (rowWidths.Length * RowHeight) + (VerticalPadding * 2);
            Rect plate = Place(anchor, bounds, plateWidth, plateHeight);

            List<Rect> rows = new(rowWidths.Length);
            for (int i = 0; i < rowWidths.Length; i++)
            {
                rows.Add(new Rect(
                    plate.X + Padding,
                    plate.Y + VerticalPadding + (i * RowHeight),
                    contentWidth,
                    RowHeight));
            }

            return new Layout(plate, rows, wrapped);
        }

        /// <summary>Measures each entry's label and value, for handing to <see cref="Measure"/>.</summary>
        private static EntryMetrics[] MetricsFor(IReadOnlyList<DragReadout.Entry> entries)
        {
            EntryMetrics[] metrics = new EntryMetrics[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                metrics[i] = new EntryMetrics(
                    Label(Localizer.AttributeName(entries[i].AttrKey), LabelBrush).Width,
                    Label(entries[i].Value, Brushes.White).Width);
            }

            return metrics;
        }

        /// <summary>Draws a readout badge: dimmed labels against bright values, wrapping when it must.</summary>
        /// <param name="context">Target drawing context.</param>
        /// <param name="entries">The label/value pairs to show.</param>
        /// <param name="anchor">Screen point the badge sits above.</param>
        /// <param name="bounds">The canvas size the plate must stay inside.</param>
        public static void DrawReadout(
            DrawingContext context,
            IReadOnlyList<DragReadout.Entry> entries,
            Point anchor,
            Size bounds)
        {
            Layout layout = Measure(
                MetricsFor(entries), Label(Separator, LabelBrush).Width, anchor, bounds);
            if (layout.Rows.Count == 0)
            {
                return;
            }

            context.FillRectangle(Plate, layout.Plate, Radius);

            if (layout.Wrapped)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DrawColumnRow(context, entries[i], layout.Rows[i]);
                }
                return;
            }

            DrawInlineRow(context, entries, layout.Rows[0]);
        }

        /// <summary>
        /// Draws a one-value badge in the shared style, centered above <paramref name="anchor"/>. Kept for
        /// callers with a bare value and no attribute to label it with.
        /// </summary>
        /// <param name="context">Target drawing context.</param>
        /// <param name="value">The value to show.</param>
        /// <param name="anchor">Screen point the badge sits above.</param>
        /// <param name="bounds">The canvas size the plate must stay inside.</param>
        public static void DrawValue(DrawingContext context, string value, Point anchor, Size bounds)
        {
            FormattedText label = Label(value, Brushes.White);
            Rect plate = Place(anchor, bounds, label.Width + (Padding * 2), Height);
            context.FillRectangle(Plate, plate, Radius);
            context.DrawText(
                label,
                new Point(plate.Center.X - (label.Width / 2), plate.Center.Y - (label.Height / 2)));
        }

        /// <summary>Draws one wrapped row: label at the left, value flushed right.</summary>
        private static void DrawColumnRow(DrawingContext context, DragReadout.Entry entry, Rect row)
        {
            FormattedText label = Label(Localizer.AttributeName(entry.AttrKey), LabelBrush);
            FormattedText value = Label(entry.Value, Brushes.White);
            context.DrawText(label, new Point(row.X, row.Center.Y - (label.Height / 2)));
            context.DrawText(
                value, new Point(row.Right - value.Width, row.Center.Y - (value.Height / 2)));
        }

        /// <summary>Draws every entry on one row, separated by the shared middot.</summary>
        private static void DrawInlineRow(
            DrawingContext context, IReadOnlyList<DragReadout.Entry> entries, Rect row)
        {
            double x = row.X;
            for (int i = 0; i < entries.Count; i++)
            {
                FormattedText label = Label(Localizer.AttributeName(entries[i].AttrKey), LabelBrush);
                FormattedText value = Label(entries[i].Value, Brushes.White);
                context.DrawText(label, new Point(x, row.Center.Y - (label.Height / 2)));
                x += label.Width + LabelValueGap;
                context.DrawText(value, new Point(x, row.Center.Y - (value.Height / 2)));
                x += value.Width;

                if (i == entries.Count - 1)
                {
                    continue;
                }

                FormattedText separator = Label(Separator, LabelBrush);
                x += SeparatorGap;
                context.DrawText(separator, new Point(x, row.Center.Y - (separator.Height / 2)));
                x += separator.Width + SeparatorGap;
            }
        }

        /// <summary>Content width of every entry laid out on one row.</summary>
        private static double InlineContentWidth(
            IReadOnlyList<EntryMetrics> entries, double separatorWidth)
        {
            double width = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                width += RowContentWidth(entries[i]);
                if (i < entries.Count - 1)
                {
                    width += (SeparatorGap * 2) + separatorWidth;
                }
            }

            return width;
        }

        /// <summary>Content width of each entry laid out on its own row.</summary>
        private static double[] RowWidths(IReadOnlyList<EntryMetrics> entries)
        {
            double[] widths = new double[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                widths[i] = RowContentWidth(entries[i]);
            }

            return widths;
        }

        /// <summary>Label plus gap plus value, the width one entry needs.</summary>
        private static double RowContentWidth(EntryMetrics entry)
        {
            return entry.LabelWidth + LabelValueGap + entry.ValueWidth;
        }

        /// <summary>
        /// Positions a plate of the given size above <paramref name="anchor"/>, clamped into
        /// <paramref name="bounds"/> and flipped below the anchor when it will not fit above.
        /// </summary>
        /// <param name="anchor">Screen point the plate sits above.</param>
        /// <param name="bounds">The canvas size the plate must stay inside.</param>
        /// <param name="width">Measured plate width in screen pixels.</param>
        /// <param name="height">Measured plate height in screen pixels.</param>
        /// <returns>The positioned plate rectangle.</returns>
        public static Rect Place(Point anchor, Size bounds, double width, double height)
        {
            double above = anchor.Y - height - Gap;
            double y = above >= 0 ? above : anchor.Y + Gap;
            y = Math.Clamp(y, 0, Math.Max(0, bounds.Height - height));

            double x = Math.Clamp(
                anchor.X - (width / 2), 0, Math.Max(0, bounds.Width - width));

            return new Rect(x, y, width, height);
        }
    }
}

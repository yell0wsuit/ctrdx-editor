using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace CtrDxEditor.Controls
{
    /// <summary>Text used by search results to highlight every query occurrence.</summary>
    public sealed class SearchHighlightTextBlock : TextBlock
    {
        /// <summary>Defines the source text to split into ordinary and matching runs.</summary>
        public static readonly StyledProperty<string> SourceTextProperty =
            AvaloniaProperty.Register<SearchHighlightTextBlock, string>(nameof(SourceText), string.Empty);

        /// <summary>Defines the case-insensitive search query to highlight.</summary>
        public static readonly StyledProperty<string> QueryProperty =
            AvaloniaProperty.Register<SearchHighlightTextBlock, string>(nameof(Query), string.Empty);

        /// <summary>Defines the background applied to matching runs.</summary>
        public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
            AvaloniaProperty.Register<SearchHighlightTextBlock, IBrush?>(nameof(HighlightBrush));

        /// <summary>Text displayed by the control.</summary>
        public string SourceText
        {
            get => GetValue(SourceTextProperty);
            set => SetValue(SourceTextProperty, value);
        }

        /// <summary>Case-insensitive query highlighted in <see cref="SourceText"/>.</summary>
        public string Query
        {
            get => GetValue(QueryProperty);
            set => SetValue(QueryProperty, value);
        }

        /// <summary>Background applied only to matching runs.</summary>
        public IBrush? HighlightBrush
        {
            get => GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        /// <summary>Splits text into ordered ordinary and matching runs.</summary>
        /// <param name="text">Visible source text.</param>
        /// <param name="query">Query whose trimmed occurrences should be marked.</param>
        /// <returns>Runs preserving the complete original text.</returns>
        public static IReadOnlyList<SearchHighlightRun> SplitRuns(string text, string query)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(query);

            string trimmedQuery = query.Trim();
            if (trimmedQuery.Length == 0)
            {
                return [new SearchHighlightRun(text, false)];
            }

            List<SearchHighlightRun> runs = [];
            int position = 0;
            while (position < text.Length)
            {
                int match = text.IndexOf(trimmedQuery, position, StringComparison.OrdinalIgnoreCase);
                if (match < 0)
                {
                    runs.Add(new SearchHighlightRun(text[position..], false));
                    break;
                }

                if (match > position)
                {
                    runs.Add(new SearchHighlightRun(text[position..match], false));
                }

                runs.Add(new SearchHighlightRun(text.Substring(match, trimmedQuery.Length), true));
                position = match + trimmedQuery.Length;
            }

            return runs;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SourceTextProperty
                || change.Property == QueryProperty
                || change.Property == HighlightBrushProperty)
            {
                RenderRuns();
            }
        }

        /// <summary>Rebuilds inline content from the current source, query, and highlight brush.</summary>
        private void RenderRuns()
        {
            InlineCollection inlines = [];
            foreach (SearchHighlightRun segment in SplitRuns(SourceText, Query))
            {
                Run run = new(segment.Text);
                if (segment.IsMatch)
                {
                    run.Background = HighlightBrush;
                    run.FontWeight = FontWeight.SemiBold;
                }

                inlines.Add(run);
            }

            Inlines = inlines;
        }
    }

    /// <summary>One plain or matching segment of search-result text.</summary>
    /// <param name="Text">Original text for the segment.</param>
    /// <param name="IsMatch">Whether the result renderer should highlight the segment.</param>
    public sealed record SearchHighlightRun(string Text, bool IsMatch);
}

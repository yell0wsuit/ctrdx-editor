using System;
using System.Collections.Generic;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>One text segment and the emphasis applied to it in Usage Guide body copy.</summary>
    /// <param name="Text">Visible text with emphasis markers removed.</param>
    /// <param name="IsBold">Whether the segment uses bold weight.</param>
    /// <param name="IsItalic">Whether the segment uses the italic Inter face.</param>
    public sealed record GuideTextRun(string Text, bool IsBold = false, bool IsItalic = false);

    /// <summary>Parses the Usage Guide's constrained Markdown-like emphasis syntax.</summary>
    public static class GuideTextParser
    {
        /// <summary>Converts bold and italic markers into ordered display runs.</summary>
        /// <param name="text">Localized Usage Guide body text.</param>
        /// <returns>Display runs in source order.</returns>
        public static IReadOnlyList<GuideTextRun> Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            List<GuideTextRun> runs = [];
            int plainStart = 0;
            int index = 0;

            while (index < text.Length)
            {
                if (text[index] != '*')
                {
                    index++;
                    continue;
                }

                int markerLength = MarkerLengthAt(text, index);
                string marker = text[index..(index + markerLength)];
                int contentStart = index + markerLength;
                int closingIndex = text.IndexOf(marker, contentStart, StringComparison.Ordinal);
                if (closingIndex <= contentStart)
                {
                    AddPlainRun(runs, text, plainStart, text.Length);
                    return runs;
                }

                AddPlainRun(runs, text, plainStart, index);
                runs.Add(new GuideTextRun(
                    text[contentStart..closingIndex],
                    IsBold: markerLength >= 2,
                    IsItalic: markerLength is 1 or 3));

                index = closingIndex + markerLength;
                plainStart = index;
            }

            AddPlainRun(runs, text, plainStart, text.Length);
            return runs;
        }

        private static int MarkerLengthAt(string text, int index)
        {
            int length = 1;
            while (length < 3 && index + length < text.Length && text[index + length] == '*')
            {
                length++;
            }

            return length;
        }

        private static void AddPlainRun(List<GuideTextRun> runs, string text, int start, int end)
        {
            if (end > start)
            {
                runs.Add(new GuideTextRun(text[start..end]));
            }
        }
    }
}

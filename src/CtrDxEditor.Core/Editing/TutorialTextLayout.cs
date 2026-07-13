using System;
using System.Collections.Generic;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Greedy word-wrap for tutorial text that packs whole words up to a measured width.</summary>
    public static class TutorialTextLayout
    {
        /// <summary>
        /// Wraps <paramref name="text"/> to <paramref name="maxWidth"/> using <paramref name="measure"/>
        /// to size candidate lines. Splits on spaces, honors explicit newlines, and never splits a single
        /// word. An over-wide word gets its own line. Returns an empty list for empty or whitespace input.
        /// </summary>
        /// <param name="text">The text to wrap.</param>
        /// <param name="maxWidth">The maximum measured width of each line.</param>
        /// <param name="measure">A function that measures a candidate line.</param>
        /// <returns>The wrapped lines.</returns>
        public static IReadOnlyList<string> Wrap(string text, double maxWidth, Func<string, double> measure)
        {
            List<string> lines = [];
            if (string.IsNullOrWhiteSpace(text))
            {
                return lines;
            }

            foreach (string paragraph in text.Replace("\r\n", "\n").Split('\n'))
            {
                string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string current = string.Empty;
                foreach (string word in words)
                {
                    string candidate = current.Length == 0 ? word : current + " " + word;
                    if (current.Length == 0 || measure(candidate) <= maxWidth)
                    {
                        current = candidate;
                    }
                    else
                    {
                        lines.Add(current);
                        current = word;
                    }
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                }
            }

            return lines;
        }
    }
}

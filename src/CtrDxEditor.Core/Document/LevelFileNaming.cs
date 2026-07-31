using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CtrDxEditor.Core.Document
{
    /// <summary>
    /// Turns a level's name into a file name a save dialog can suggest. The name is author-supplied free
    /// text, and levels move between machines, so it is sanitized against the strictest rules of the
    /// platforms the editor runs on rather than only the current one.
    /// </summary>
    public static class LevelFileNaming
    {
        /// <summary>The name used when a level has none, or when sanitizing leaves nothing usable.</summary>
        public const string Fallback = "level";

        /// <summary>The character substituted for anything a file name cannot contain.</summary>
        public const char Replacement = '_';

        /// <summary>
        /// Longest suggested base name. Well under any file-system limit, since the name is only a starting
        /// point the author can extend in the dialog, and an overlong default is unwieldy to edit.
        /// </summary>
        public const int MaxLength = 64;

        /// <summary>
        /// Characters Windows rejects in a file name. <see cref="Path.GetInvalidFileNameChars"/> reports
        /// these when the editor runs on Windows but only the separator and NUL on macOS and Linux, so they
        /// are unioned in below: a level named on a Mac still has to be saveable on Windows.
        /// </summary>
        private const string WindowsInvalidChars = "<>:\"/\\|?*";

        /// <summary>Everything replaced on sight, from the running platform's rules plus Windows'.</summary>
        private static readonly SearchValues<char> Forbidden = SearchValues.Create(
            new string([.. new HashSet<char>([.. Path.GetInvalidFileNameChars(), .. WindowsInvalidChars])]));

        /// <summary>
        /// Names Windows reserves for devices, which it refuses as a file name whatever the extension. The
        /// BCL exposes no equivalent check, and the list is fixed by the OS.
        /// </summary>
        private static readonly HashSet<string> ReservedNames =
        [
            with(StringComparer.OrdinalIgnoreCase),
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        ];

        /// <summary>
        /// The base file name for a level name: forbidden and control characters become
        /// <see cref="Replacement"/>, surrounding whitespace and trailing dots are dropped (Windows silently
        /// strips them), the result is capped at <see cref="MaxLength"/>, and a reserved device name gains a
        /// trailing <see cref="Replacement"/>. Returns <see cref="Fallback"/> when nothing usable remains.
        /// </summary>
        /// <param name="levelName">The level's name, which may be null, blank, or anything the author typed.</param>
        /// <returns>A non-empty base name with no extension.</returns>
        public static string Sanitize(string? levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
            {
                return Fallback;
            }

            StringBuilder builder = new(levelName.Length);
            foreach (char c in levelName)
            {
                _ = builder.Append(char.IsControl(c) || Forbidden.Contains(c) ? Replacement : c);
            }

            string name = builder.ToString().Trim();
            if (name.Length > MaxLength)
            {
                name = name[..MaxLength];
            }

            // Trailing dots and spaces are trimmed rather than replaced: Windows drops them from the name it
            // actually creates, so keeping them would mean writing to a file named differently than shown.
            name = name.TrimEnd(' ', '.');

            return name.Length == 0
                ? Fallback
                : ReservedNames.Contains(name) ? name + Replacement : name;
        }

        /// <summary>
        /// The file name to suggest for a level, as <c>&lt;sanitized name&gt;.&lt;extension&gt;</c>.
        /// </summary>
        /// <param name="levelName">The level's name; blank falls back to <see cref="Fallback"/>.</param>
        /// <param name="extension">The extension to append, without a leading dot.</param>
        /// <returns>A file name suitable for a save dialog's suggestion.</returns>
        public static string Suggest(string? levelName, string extension)
        {
            return $"{Sanitize(levelName)}.{extension}";
        }
    }
}

using System;
using System.Globalization;

namespace CtrDxEditor.Playtest
{
    /// <summary>
    /// Parses the handshake line Cut the Rope: DX writes to stdout the moment it accepts a
    /// <c>--level</c> launch.
    /// </summary>
    /// <remarks>
    /// The line is the editor's only positive proof that the launched program is Cut the Rope: DX and
    /// new enough to understand <c>--level</c>: an older build, or an unrelated program, silently ignores
    /// the switch and never emits it. The game writes <c>ctrdx-playtest &lt;protocol&gt; &lt;version&gt;</c>;
    /// this mirrors that contract (see the game's <c>PlaytestHandshake</c> for the emitter).
    /// </remarks>
    public static class PlaytestHandshakeLine
    {
        /// <summary>Leading token that identifies a handshake line. Must match the game's emitter exactly.</summary>
        public const string Signature = "ctrdx-playtest";

        /// <summary>Attempts to parse one line of process output as a handshake.</summary>
        /// <param name="line">A single line from the game's standard output.</param>
        /// <param name="protocol">The handshake format version the game declared, or 0 on failure.</param>
        /// <param name="version">The game's build version string, or an empty string on failure.</param>
        /// <returns>True when <paramref name="line"/> is a well-formed handshake.</returns>
        public static bool TryParse(string? line, out int protocol, out string version)
        {
            protocol = 0;
            version = "";

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            // At most three parts: signature, protocol, and the version, which is kept whole so a version
            // string that ever carries a space survives intact.
            string[] parts = line.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3
                || !string.Equals(parts[0], Signature, StringComparison.Ordinal)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedProtocol))
            {
                return false;
            }

            protocol = parsedProtocol;
            version = parts[2].Trim();
            return version.Length > 0;
        }
    }
}

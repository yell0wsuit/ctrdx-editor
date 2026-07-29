using System;

namespace CtrDxEditor.Update
{
    /// <summary>
    /// Version arithmetic behind the startup update check: which builds may ask, and whether a
    /// published release tag is actually newer than the running build.
    /// </summary>
    /// <remarks>
    /// Deliberately free of I/O so the comparison rules can be tested without touching the network.
    /// </remarks>
    public static class UpdateVersion
    {
        /// <summary>
        /// Whether a build is a published release, and so may compare itself against GitHub.
        /// </summary>
        /// <param name="informational">Informational version, e.g. <c>1.0.0</c> or <c>1.0.0-dirty+3dff1f1</c>.</param>
        /// <returns><see langword="true"/> for a bare <c>major.minor.patch</c> build.</returns>
        /// <remarks>
        /// The distribution scripts publish with <c>-p:VersionSuffix=</c>, so a release stamps a bare
        /// <c>1.0.0</c>; every other build carries the <c>-dirty+&lt;commit&gt;</c> marker from
        /// <c>Directory.Build.props</c>. Without this gate a developer running the current source at the
        /// already-released version number would be told to update on every launch.
        /// </remarks>
        public static bool IsReleaseBuild(string? informational)
        {
            return informational is not null
                && !informational.Contains('-', StringComparison.Ordinal)
                && !informational.Contains('+', StringComparison.Ordinal)
                && Parse(informational) is not null;
        }

        /// <summary>
        /// Parses a release tag or version string into a comparable version.
        /// </summary>
        /// <param name="value">Tag or version, with or without the leading <c>v</c> (e.g. <c>v1.2.3</c>).</param>
        /// <returns>The parsed version, or <see langword="null"/> when it is not numeric and dotted.</returns>
        /// <remarks>
        /// The release workflow tags <c>v${version}</c>, so the prefix is stripped before parsing. A
        /// trailing fourth component is accepted and dropped, because the two sides of the comparison
        /// can disagree on arity - <see cref="System.Reflection.AssemblyName.Version"/> renders
        /// <c>1.0.0.0</c> - and <see cref="Version"/> orders an absent revision *below* a zero one, which
        /// would report a phantom update. Anything else - a renamed scheme, a truncated response - yields
        /// null and is treated as "no update", the safe direction for a prompt the user cannot suppress.
        /// </remarks>
        public static Version? Parse(string? value)
        {
            if (value is null)
            {
                return null;
            }

            ReadOnlySpan<char> text = value.AsSpan().Trim();
            if (text.Length > 0 && (text[0] is 'v' or 'V'))
            {
                text = text[1..];
            }

            // Version.TryParse also accepts two-part forms and a leading sign; releases are always
            // three-part, so the shape is checked here rather than silently reinterpreted.
            int parts = 1;
            foreach (char c in text)
            {
                if (c == '.')
                {
                    parts++;
                }
                else if (!char.IsAsciiDigit(c))
                {
                    return null;
                }
            }

            return (parts is 3 or 4) && Version.TryParse(text, out Version? version)
                ? new Version(version.Major, version.Minor, version.Build)
                : null;
        }

        /// <summary>
        /// Whether a published release tag supersedes the running build.
        /// </summary>
        /// <param name="localInformational">Informational version of the running build.</param>
        /// <param name="remoteTag">Tag name of the latest published release, e.g. <c>v1.0.1</c>.</param>
        /// <returns><see langword="true"/> only when this is a release build and the tag is strictly newer.</returns>
        public static bool IsNewer(string? localInformational, string? remoteTag)
        {
            if (!IsReleaseBuild(localInformational))
            {
                return false;
            }

            Version? local = Parse(localInformational);
            Version? remote = Parse(remoteTag);
            return local is not null && remote is not null && remote > local;
        }
    }
}

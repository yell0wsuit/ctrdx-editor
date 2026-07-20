using System;
using System.Reflection;

namespace CtrDxEditor
{
    /// <summary>
    /// The editor's build version, as stamped by MSBuild into the assembly.
    /// </summary>
    /// <remarks>
    /// Dev builds carry a <c>-dirty+&lt;commit&gt;</c> suffix (see <c>VersionSuffix</c> and
    /// <c>IncludeSourceRevisionInInformationalVersion</c> in <c>Directory.Build.props</c>);
    /// release builds clear the suffix and render as a bare <c>1.2.3</c>.
    /// </remarks>
    public static class AppVersion
    {
        /// <summary>Number of commit hash characters kept for display.</summary>
        private const int ShortCommitLength = 7;

        /// <summary>Version string for the title bar, with the commit hash shortened.</summary>
        public static string Display { get; } = Shorten(
            typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(AppVersion).Assembly.GetName().Version?.ToString());

        /// <summary>
        /// Trims the full commit hash in an informational version down to
        /// <see cref="ShortCommitLength"/> characters, leaving versions without one untouched.
        /// </summary>
        /// <param name="informational">Informational version, e.g. <c>1.0.0-dirty+3dff1f181b3e…</c>.</param>
        /// <returns>The display form, or <c>"unknown"</c> when no version is available.</returns>
        public static string Shorten(string? informational)
        {
            if (string.IsNullOrEmpty(informational))
            {
                return "unknown";
            }

            int plus = informational.IndexOf('+', StringComparison.Ordinal);
            if (plus < 0)
            {
                return informational;
            }

            ReadOnlySpan<char> commit = informational.AsSpan(plus + 1);
            return commit.Length <= ShortCommitLength
                ? informational
                : string.Concat(informational.AsSpan(0, plus + 1), commit[..ShortCommitLength]);
        }
    }
}

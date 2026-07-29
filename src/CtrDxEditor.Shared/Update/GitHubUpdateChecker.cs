using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;

namespace CtrDxEditor.Update
{
    /// <summary>Asks GitHub whether a newer stable release of the editor has been published.</summary>
    /// <remarks>
    /// Wired only into heads that ship self-updating builds. The browser head always serves whatever
    /// is deployed, so it leaves <c>PlatformStartup.CheckForUpdate</c> null and never references this
    /// type - which also keeps it, and its HTTP and JSON dependencies, out of the trimmed wasm payload.
    /// </remarks>
    public static class GitHubUpdateChecker
    {
        /// <summary>Releases endpoint for the editor's repository, filtered to the latest stable release.</summary>
        /// <remarks>
        /// GitHub defines <c>/releases/latest</c> as the newest release that is neither a draft nor a
        /// prerelease, so betas published from this repository do not prompt anyone to "update".
        /// </remarks>
        private const string LatestReleaseUrl =
            "https://api.github.com/repos/yell0wsuit/ctrdx-editor/releases/latest";

        /// <summary>The release page "Yes" opens; GitHub redirects it to the newest stable release.</summary>
        public const string ReleasesUrl = "https://github.com/yell0wsuit/ctrdx-editor/releases/latest";

        /// <summary>How long the check may take before it is abandoned as a failure.</summary>
        /// <remarks>Short on purpose: nothing waits on the result, but a stalled request should not
        /// surface a prompt minutes into a session, long after the user has started working.</remarks>
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Reports whether GitHub's latest stable release is newer than the running build.
        /// </summary>
        /// <param name="localVersion">Informational version of the running build.</param>
        /// <param name="ct">Cancels the check.</param>
        /// <returns><see langword="true"/> only when an update definitely exists.</returns>
        /// <remarks>
        /// Never throws. Every failure mode - offline, DNS, rate limiting (the unauthenticated API
        /// allows 60 requests an hour per address), a malformed body - answers "no update", because a
        /// background check the user did not ask for must never interrupt startup with an error.
        /// </remarks>
        public static async Task<bool> IsUpdateAvailableAsync(string? localVersion, CancellationToken ct = default)
        {
            // Checked before the request, not after: a build from source is never eligible, so there is
            // no reason to spend a network round trip - or a rate-limit slot - discovering that.
            if (!UpdateVersion.IsReleaseBuild(localVersion))
            {
                return false;
            }

            try
            {
                using HttpClient http = new() { Timeout = Timeout };
                // GitHub rejects API requests without a User-Agent.
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"CtrDxEditor/{localVersion}");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                GitHubRelease? release = await http.GetFromJsonAsync(
                    LatestReleaseUrl, AppJsonContext.Default.GitHubRelease, ct);

                return UpdateVersion.IsNewer(localVersion, release?.TagName);
            }
            catch (Exception ex)
            {
                // Logged rather than surfaced: the check is best-effort, and the user gains nothing
                // from a dialog explaining that an update check they never requested did not complete.
                Console.WriteLine($"[CtrDx] Update check failed; continuing without it.\n{ex}");
                return false;
            }
        }
    }
}

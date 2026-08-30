using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests the service worker's half of Avalonia's streamed-save protocol, which is what "Save
    /// level as" runs through in Firefox.
    /// </summary>
    public class ServiceWorkerFileSaveTests
    {
        /// <summary>
        /// The worker asks for chunks from a pull, not from an acknowledgement of one already
        /// received.
        /// </summary>
        /// <remarks>
        /// The polyfill's sink returns its ready promise from its own start, so the editor's very
        /// first write is held until the worker pulls. Avalonia's sw.js posts that pull only after
        /// a chunk arrives, so the two wait on each other and the download - already started by
        /// the polyfill's hidden iframe - reads a stream that stays empty and saves nothing.
        /// </remarks>
        [Fact]
        public void FileSaveSourcePullsRatherThanAcknowledging()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Browser", "wwwroot", "sw-file-save.js"));

            // Without this the editor's first write is never released and the save deadlocks at zero bytes.
            Assert.Contains("pull() {", source, StringComparison.Ordinal);
            Assert.Contains("this.port.postMessage({ type: PULL });", source, StringComparison.Ordinal);

            // The pull is the only signal back to the page, so the chunk branch enqueues and nothing more.
            const string enqueue = "this.controller.enqueue(data.chunk);";
            int chunk = source.IndexOf(enqueue, StringComparison.Ordinal);
            Assert.NotEqual(-1, chunk);
            int nextBranch = source.IndexOf("} else if", chunk, StringComparison.Ordinal);
            Assert.DoesNotContain("postMessage", source[chunk..nextBranch], StringComparison.Ordinal);
        }

        /// <summary>
        /// Both workers speak the protocol. The polyfill streams through whichever registration it
        /// finds without checking what it does, so the inert development one has to as well.
        /// </summary>
        [Theory]
        [InlineData("service-worker.js")]
        [InlineData("service-worker.published.js")]
        public void EveryServiceWorkerImplementsTheSaveProtocol(string worker)
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Browser", "wwwroot", worker));

            Assert.Contains("importScripts(\"./sw-file-save.js\")", source, StringComparison.Ordinal);
            Assert.Contains("offerFileSave(", source, StringComparison.Ordinal);
            Assert.Contains("respondToFileSave(", source, StringComparison.Ordinal);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}

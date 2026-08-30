using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests the override that keeps Avalonia's save-file polyfill on its Blob download.
    /// </summary>
    /// <remarks>
    /// The polyfill picks its delivery path on whether it finds a service worker registration, and
    /// the streamed path it picks when it does cannot work: the worker is never pulled from, and
    /// the chunk the polyfill posts is a view onto the WebAssembly heap, which cannot be
    /// transferred. Registering a worker for offline support is what exposed that, so the page has
    /// to hide the registration from the polyfill.
    /// </remarks>
    public class BrowserSaveFallbackTests
    {
        /// <summary>
        /// The override hides the registration, and only where the polyfill is actually used.
        /// </summary>
        /// <remarks>
        /// Chromium has the native picker and never enters the polyfill, so it must not be given a
        /// stubbed getRegistration - the check mirrors Avalonia's own Caniuse.hasNativeFilePicker.
        /// </remarks>
        [Fact]
        public void SaveFallbackHidesTheRegistrationFromThePolyfill()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Browser", "wwwroot", "save-fallback.js"));

            Assert.Contains("!(\"showSaveFilePicker\" in globalThis)", source, StringComparison.Ordinal);
            Assert.Contains("\"getRegistration\"", source, StringComparison.Ordinal);
            Assert.Contains("Promise.resolve(undefined)", source, StringComparison.Ordinal);
        }

        /// <summary>The page loads the override, or saving silently reverts to the broken path.</summary>
        [Fact]
        public void IndexHtmlLoadsTheSaveFallback()
        {
            string index = File.ReadAllText(SourcePath("CtrDxEditor.Browser", "wwwroot", "index.html"));

            Assert.Contains("src=\"./save-fallback.js\"", index, StringComparison.Ordinal);
        }

        /// <summary>
        /// The workers stay out of the save path. Speaking the streamed protocol would only invite
        /// the polyfill back onto a route that cannot deliver bytes.
        /// </summary>
        [Theory]
        [InlineData("service-worker.js")]
        [InlineData("service-worker.published.js")]
        public void ServiceWorkersStayOutOfTheSavePath(string worker)
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Browser", "wwwroot", worker));

            Assert.DoesNotContain("FileSave", source, StringComparison.Ordinal);
            Assert.DoesNotContain("readablePort", source, StringComparison.Ordinal);
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

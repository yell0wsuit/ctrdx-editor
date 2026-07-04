using System;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests XML-string level IO on the editor view model (browser-safe, no file paths).</summary>
    public class EditorLevelIoTests
    {
        private sealed class EmptyStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(false);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult(Array.Empty<byte>());
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult("");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>Verifies that loading an XML string and serializing it back preserves the document structure.</summary>
        [Fact]
        public void LoadLevelXmlThenToXmlPreservesDocument()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            const string xml = "<?xml version='1.0' encoding='utf-8'?>\n<map>\n    <layer name=\"settings\">\n        <map gridSize=\"32\" width=\"100\" height=\"80\" />\n    </layer>\n</map>";

            vm.LoadLevelXml(xml);

            Assert.NotNull(vm.Document);
            Assert.Equal(100, vm.Document.Width);
            Assert.Equal(80, vm.Document.Height);
            string? saved = vm.ToXml();
            Assert.NotNull(saved);
            Assert.Contains("width=\"100\"", saved);
        }

        /// <summary>Verifies that loading XML notifies the view after the document has been replaced.</summary>
        [Fact]
        public void LoadLevelXmlRaisesLevelLoadedAfterDocumentIsAvailable()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            const string xml = "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer></map>";
            bool raised = false;

            vm.LevelLoaded += () =>
            {
                Assert.NotNull(vm.Document);
                raised = true;
            };

            vm.LoadLevelXml(xml);

            Assert.True(raised);
        }
    }
}

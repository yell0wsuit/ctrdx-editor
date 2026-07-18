using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            Assert.True(vm.HasDocument);
            Assert.NotNull(vm.Document);
            Assert.Equal(100, vm.Document.Width);
            Assert.Equal(80, vm.Document.Height);
            string? saved = vm.ToXml();
            Assert.NotNull(saved);
            Assert.Contains("width=\"100\"", saved);
        }

        /// <summary>Verifies that closing a level resets the editable document state.</summary>
        [Fact]
        public void CloseLevelClearsDocumentAndEditorState()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            const string xml = "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer></map>";
            vm.LoadLevelXml(xml);
            _ = vm.PlaceObject("target", 50, 60);
            vm.ToggleLock(vm.SelectedObject);

            vm.CloseLevel();

            Assert.False(vm.HasDocument);
            Assert.Null(vm.Document);
            Assert.Null(vm.SelectedObject);
            Assert.Null(vm.LockedObject);
            Assert.Empty(vm.Layers);
            Assert.Empty(vm.Palette);
            Assert.Empty(vm.Fields);
            Assert.Null(vm.ToXml());
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

        /// <summary>Legacy candy and bulb keys are normalized to 0-based ids while preserving grab targets.</summary>
        [Fact]
        public void LoadLevelXmlNormalizesBindingKeysAndRetargetsGrabs()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            const string xml = """
                <map>
                  <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                    <gameDesign twoParts="false" nightLevel="true" />
                  </layer>
                  <layer name="Objects">
                    <candy x="10" y="10" candyNumber="7" />
                    <candy x="20" y="20" candyNumber="9" />
                    <lightBulb x="30" y="30" bulbNumber="4" />
                    <lightBulb x="40" y="40" bulbNumber="8" />
                    <grab x="5" y="5" candyNumber="9" />
                    <grab x="6" y="6" bindBulb="true" bulbNumber="4" />
                  </layer>
                </map>
                """;

            vm.LoadLevelXml(xml);

            XDocument saved = XDocument.Parse(vm.ToXml()!);
            XElement[] objects = [.. saved.Descendants("layer")
                .Single(l => (string?)l.Attribute("name") == "Objects")
                .Elements()];

            Assert.Equal(["0", "1"], objects.Where(e => e.Name == "candy").Attributes("candyNumber").Select(a => a.Value));
            Assert.Equal(["0", "1"], objects.Where(e => e.Name == "lightBulb").Attributes("bulbNumber").Select(a => a.Value));
            Assert.Equal("1", objects.First(e => e.Name == "grab").Attribute("candyNumber")?.Value);
            Assert.Equal("0", objects.Last(e => e.Name == "grab").Attribute("bulbNumber")?.Value);
        }
    }
}

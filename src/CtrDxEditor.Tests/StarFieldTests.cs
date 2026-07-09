using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for star-specific property panel fields.</summary>
    public class StarFieldTests
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

        private const string Level = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <star x="100" y="100" timeout="-1" />
            </layer>
        </map>
        """;

        /// <summary>Checking spin on a star writes a default positive whole rotateSpeed and reveals controls.</summary>
        [Fact]
        public void CheckingStarSpinWritesDefaultRotateSpeed()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.Objects[0];
            vm.SelectedObject = star;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;

            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            Assert.Equal("70", vm.Fields.Single(f => f.Name == "spinSpeed").Value);
            Assert.True(vm.Fields.Single(f => f.Name == "spinClockwise").BoolValue);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the lantern property panel: candyCaptured plus the shared movement controls.</summary>
    public class LanternFieldTests
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
                <map gridSize="32" width="320" height="480" />
            </layer>
            <layer name="Objects">
                <lantern x="100" y="100" candyCaptured="false" />
            </layer>
        </map>
        """;

        /// <summary>The lantern panel exposes its authored candy-capture state as a toggle.</summary>
        [Fact]
        public void LanternExposesCandyCapturedToggle()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            vm.SelectedObject = vm.Document!.Objects.Single(o => o.Type == "lantern");

            Assert.Contains(vm.Fields, f => f.Name == "candyCaptured" && f.IsBool);
        }

        /// <summary>The lantern panel reuses the shared mover-path mode controls.</summary>
        [Fact]
        public void LanternExposesMovementControls()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            vm.SelectedObject = vm.Document!.Objects.Single(o => o.Type == "lantern");

            Assert.Contains(vm.Fields, f => f.Name == "movementMode" && f.EnumOptions is not null);
        }
    }
}

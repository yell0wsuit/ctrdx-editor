using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for spike-specific property panel fields.</summary>
    public class SpikeFieldTests
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
                <spike1 x="100" y="100" angle="0" size="1" toggled="false" />
            </layer>
        </map>
        """;

        /// <summary>Untoggled spikes expose the checkbox but not the group dropdown.</summary>
        [Fact]
        public void UntoggledSpikeHidesGroupField()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            vm.SelectedObject = vm.Document!.Objects[0];

            Assert.Contains(vm.Fields, f => f.Name == "toggled" && f.IsBool);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "toggleGroup");
        }

        /// <summary>Checking the spike toggle reveals the group dropdown and writes group 1 by default.</summary>
        [Fact]
        public void CheckingToggledShowsGroupField()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject spike = vm.Document!.Objects[0];
            vm.SelectedObject = spike;

            vm.Fields.Single(f => f.Name == "toggled").BoolValue = true;

            Assert.Equal("1", spike.GetAttr("toggled"));
            AttributeFieldViewModel group = vm.Fields.Single(f => f.Name == "toggleGroup");
            Assert.Equal(["1", "2"], group.EnumOptions!.Select(o => o.Value));
            Assert.Equal("1", group.Value);
        }

        /// <summary>Changing spike size through the field renames the XML element to the matching spikeN.</summary>
        [Fact]
        public void SizeFieldRenamesSpikeElement()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject spike = vm.Document!.Objects[0];
            vm.SelectedObject = spike;

            vm.Fields.Single(f => f.Name == "size").SelectedOption =
                vm.Fields.Single(f => f.Name == "size").EnumOptions!.Single(o => o.Value == "4");

            Assert.Equal("spike4", spike.Type);
            Assert.Equal("4", spike.GetAttr("size"));
        }
    }
}

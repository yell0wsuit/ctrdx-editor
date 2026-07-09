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
            Assert.Equal(["0", "1", "2"], group.EnumOptions!.Select(o => o.Value));
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

        /// <summary>Spike spin fields expose rotateSpeed as enabled, positive speed, and sign-backed direction.</summary>
        [Fact]
        public void SpikeSpinFieldsMapRotateSpeedSign()
        {
            const string spinningLevel = """
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                </layer>
                <layer name="Objects">
                    <spike2 x="344" y="257" angle="0" size="2" path="0,0" rotateSpeed="-130" />
                </layer>
            </map>
            """;
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(spinningLevel);
            LevelObject spike = vm.Document!.Objects[0];
            vm.SelectedObject = spike;

            Assert.True(vm.Fields.Single(f => f.Name == "spin").BoolValue);
            AttributeFieldViewModel speed = vm.Fields.Single(f => f.Name == "spinSpeed");
            Assert.True(speed.IsNumeric);
            Assert.False(speed.AllowsDecimal);
            Assert.Equal(1, speed.NumericMinimum);
            Assert.Equal("130", speed.Value);
            Assert.False(vm.Fields.Single(f => f.Name == "spinClockwise").BoolValue);

            vm.Fields.Single(f => f.Name == "spinClockwise").BoolValue = true;

            Assert.Equal("130", spike.GetAttr("rotateSpeed"));

            vm.Fields.Single(f => f.Name == "spin").BoolValue = false;

            Assert.Null(spike.GetAttr("rotateSpeed"));
            Assert.Equal("0,0", spike.GetAttr("path"));
        }

        /// <summary>Toggled spikes expose spin as unavailable so button rotation and continuous spin do not combine.</summary>
        [Fact]
        public void ToggledSpikeDisablesSpinField()
        {
            const string toggledLevel = """
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                </layer>
                <layer name="Objects">
                    <spike2 x="344" y="257" angle="0" size="2" toggled="1" />
                </layer>
            </map>
            """;
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(toggledLevel);
            vm.SelectedObject = vm.Document!.Objects[0];

            AttributeFieldViewModel spin = vm.Fields.Single(f => f.Name == "spin");

            Assert.False(spin.BoolValue);
            Assert.False(spin.IsEnabled);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "spinSpeed");
        }

        /// <summary>Enabling spin on a spike clears toggled state and preserves the existing path attribute.</summary>
        [Fact]
        public void EnablingSpikeSpinClearsToggledState()
        {
            const string level = """
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings">
                    <map gridSize="32" width="640" height="480" />
                </layer>
                <layer name="Objects">
                    <spike2 x="344" y="257" angle="0" size="2" path="0,0" toggled="1" />
                </layer>
            </map>
            """;
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(level);
            LevelObject spike = vm.Document!.Objects[0];
            vm.SelectedObject = spike;

            vm.Fields.Single(f => f.Name == "toggled").BoolValue = false;
            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;

            Assert.Equal("false", spike.GetAttr("toggled"));
            Assert.Equal("70", spike.GetAttr("rotateSpeed"));
            Assert.Equal("0,0", spike.GetAttr("path"));
            Assert.False(vm.Fields.Single(f => f.Name == "toggled").IsEnabled);
        }
    }
}

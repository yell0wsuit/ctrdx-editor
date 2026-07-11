using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests bouncer-specific property panel fields.</summary>
    public class BouncerFieldTests
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
                <bouncer1 x="100" y="100" angle="15" size="1" />
            </layer>
        </map>
        """;

        /// <summary>The width selector exposes both game sizes and keeps element and attribute synchronized.</summary>
        [Fact]
        public void SizeFieldRenamesBouncerElement()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject bouncer = vm.Document!.Objects[0];
            vm.SelectedObject = bouncer;
            AttributeFieldViewModel size = vm.Fields.Single(f => f.Name == "size");
            AttributeOptionViewModel[] options = Assert.IsType<AttributeOptionViewModel[]>(size.EnumOptions);

            Assert.Equal(["1", "2"], options.Select(o => o.Value));

            size.SelectedOption = options.Single(o => o.Value == "2");

            Assert.Equal("bouncer2", bouncer.Type);
            Assert.Equal("2", bouncer.GetAttr("size"));
        }

        /// <summary>Bouncer properties retain the ordinary numeric angle field used by the rotation dial.</summary>
        [Fact]
        public void BouncerExposesAngleField()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            vm.SelectedObject = vm.Document!.Objects[0];

            AttributeFieldViewModel angle = vm.Fields.Single(f => f.Name == "angle");
            Assert.True(angle.IsNumeric);
            Assert.Equal("15", angle.Value);
        }

        /// <summary>DX parses bouncers as movers, so their panel exposes spin and movement controls.</summary>
        [Fact]
        public void BouncerExposesMoverFields()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            vm.SelectedObject = vm.Document!.Objects[0];

            Assert.Contains(vm.Fields, f => f.Name == "spin" && f.IsBool);
            Assert.Contains(vm.Fields, f => f.Name == "movementMode" && f.EnumOptions is not null);
        }
    }
}

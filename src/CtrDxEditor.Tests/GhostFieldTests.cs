using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests ghost-specific property panel fields.</summary>
    public class GhostFieldTests
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

        private static EditorViewModel Load(string ghostXml)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml($"""
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings"><map gridSize="32" width="640" height="480" /></layer>
                <layer name="Objects">{ghostXml}</layer>
            </map>
            """);
            vm.SelectedObject = vm.Document!.Objects[0];
            return vm;
        }

        /// <summary>Radius is shown only when grab is on.</summary>
        [Fact]
        public void RadiusFieldVisibleOnlyWhenGrabOn()
        {
            EditorViewModel on = Load("<ghost x='1' y='1' grab='true' bouncer='false' />");
            Assert.Contains(on.Fields, f => f.Name == "radius");

            EditorViewModel off = Load("<ghost x='1' y='1' grab='false' bouncer='false' />");
            Assert.DoesNotContain(off.Fields, f => f.Name == "radius");
        }

        /// <summary>Angle is shown only when bouncer is on.</summary>
        [Fact]
        public void AngleFieldVisibleOnlyWhenBouncerOn()
        {
            EditorViewModel on = Load("<ghost x='1' y='1' grab='false' bouncer='true' />");
            Assert.Contains(on.Fields, f => f.Name == "angle");

            EditorViewModel off = Load("<ghost x='1' y='1' grab='false' bouncer='false' />");
            Assert.DoesNotContain(off.Fields, f => f.Name == "angle");
        }

        /// <summary>Toggling grab on rebuilds the panel so the radius field appears.</summary>
        [Fact]
        public void TogglingGrabRevealsRadius()
        {
            EditorViewModel vm = Load("<ghost x='1' y='1' grab='false' bouncer='false' />");
            AttributeFieldViewModel grab = vm.Fields.Single(f => f.Name == "grab");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "radius");

            grab.Value = "true";

            Assert.Contains(vm.Fields, f => f.Name == "radius");
        }
    }
}

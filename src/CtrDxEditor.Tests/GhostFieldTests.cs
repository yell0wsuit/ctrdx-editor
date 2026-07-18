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
            vm.SelectedObject = vm.Document!.AllObjects[0];
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

        /// <summary>Toggling grab on rebuilds the panel, reveals radius, and defaults it to 50.</summary>
        [Fact]
        public void TogglingGrabOnRevealsRadiusDefault50()
        {
            EditorViewModel vm = Load("<ghost x='1' y='1' grab='false' bouncer='false' radius='-1' />");
            AttributeFieldViewModel grab = vm.Fields.Single(f => f.Name == "grab");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "radius");

            grab.Value = "true";

            Assert.Contains(vm.Fields, f => f.Name == "radius");
            Assert.Equal("50", vm.Document!.AllObjects[0].GetAttr("radius"));
        }

        /// <summary>Toggling grab off sets radius to the -1 auto-rope sentinel and hides the field.</summary>
        [Fact]
        public void TogglingGrabOffSetsRadiusMinusOne()
        {
            EditorViewModel vm = Load("<ghost x='1' y='1' grab='true' bouncer='false' radius='50' />");
            AttributeFieldViewModel grab = vm.Fields.Single(f => f.Name == "grab");

            grab.Value = "false";

            Assert.DoesNotContain(vm.Fields, f => f.Name == "radius");
            Assert.Equal("-1", vm.Document!.AllObjects[0].GetAttr("radius"));
        }
    }
}

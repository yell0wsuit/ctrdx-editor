using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Converters;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the shared live-animation preview state exposed by the editor view model.</summary>
    public class EditorAnimationPreviewTests
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

        private static EditorViewModel Loaded()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>"
                + "<layer name=\"Objects\"><star x=\"20\" y=\"30\" rotateSpeed=\"70\" /><spike1 x=\"50\" y=\"60\" rotateSpeed=\"-40\" /></layer></map>");
            return vm;
        }

        /// <summary>Global playback toggles between all-object playback and the stopped authored view.</summary>
        [Fact]
        public void ToggleAllPreviewPlaysAndStopsAllObjects()
        {
            EditorViewModel vm = Loaded();
            LevelObject[] objects = [.. vm.Layers.SelectMany(layer => layer.Objects)];
            LevelObject star = objects[0];
            LevelObject spike = objects[1];

            vm.ToggleAnimationPreviewAll();

            Assert.Equal(AnimationPreviewMode.All, vm.AnimationPreviewMode);
            Assert.True(vm.IsAnimationPreviewing(star));
            Assert.True(vm.IsAnimationPreviewing(spike));

            vm.ToggleAnimationPreviewAll();

            Assert.Equal(AnimationPreviewMode.Off, vm.AnimationPreviewMode);
            Assert.False(vm.IsAnimationPreviewing(star));
            Assert.False(vm.IsAnimationPreviewing(spike));
        }

        /// <summary>Per-object playback switches focus and a second click on the same object stops preview.</summary>
        [Fact]
        public void ToggleObjectPreviewSwitchesObjectAndStopsSameObject()
        {
            EditorViewModel vm = Loaded();
            LevelObject[] objects = [.. vm.Layers.SelectMany(layer => layer.Objects)];
            LevelObject star = objects[0];
            LevelObject spike = objects[1];

            vm.ToggleAnimationPreviewObject(star);

            Assert.Equal(AnimationPreviewMode.Focused, vm.AnimationPreviewMode);
            Assert.Same(star, vm.AnimationPreviewObject);
            Assert.True(vm.IsAnimationPreviewing(star));
            Assert.False(vm.IsAnimationPreviewing(spike));

            vm.ToggleAnimationPreviewObject(spike);

            Assert.Equal(AnimationPreviewMode.Focused, vm.AnimationPreviewMode);
            Assert.Same(spike, vm.AnimationPreviewObject);
            Assert.False(vm.IsAnimationPreviewing(star));
            Assert.True(vm.IsAnimationPreviewing(spike));

            vm.ToggleAnimationPreviewObject(spike);

            Assert.Equal(AnimationPreviewMode.Off, vm.AnimationPreviewMode);
            Assert.Null(vm.AnimationPreviewObject);
        }

        /// <summary>Document replacement clears playback state because object identities no longer apply.</summary>
        [Fact]
        public void LoadingOrClosingLevelStopsAnimationPreview()
        {
            EditorViewModel vm = Loaded();
            vm.ToggleAnimationPreviewObject(vm.Layers[0].Objects[0]);
            vm.AnimationPreviewElapsedSeconds = 1.25;

            vm.LoadLevelXml("<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer></map>");

            Assert.Equal(AnimationPreviewMode.Off, vm.AnimationPreviewMode);
            Assert.Null(vm.AnimationPreviewObject);
            Assert.Equal(0.0, vm.AnimationPreviewElapsedSeconds);

            vm.ToggleAnimationPreviewAll();
            vm.CloseLevel();

            Assert.Equal(AnimationPreviewMode.Off, vm.AnimationPreviewMode);
        }

        /// <summary>Orbit-only objects expose the same row preview affordance as rotateSpeed objects.</summary>
        [Fact]
        public void OrbitOnlyObjectHasAnimationPreviewAvailable()
        {
            LevelObject star = new(new XElement("star",
                new XAttribute("x", "20"),
                new XAttribute("y", "30"),
                new XAttribute("path", "RC30"),
                new XAttribute("moveSpeed", "70")));

            object? available = SpinPreviewConverters.Available.Convert(
                star,
                typeof(bool),
                parameter: null,
                CultureInfo.InvariantCulture);

            Assert.True(available is true);
        }

        /// <summary>Plain point mover paths expose the same row preview affordance as RC/RW orbit paths.</summary>
        [Fact]
        public void PlainPathObjectHasAnimationPreviewAvailable()
        {
            LevelObject star = new(new XElement("star",
                new XAttribute("x", "20"),
                new XAttribute("y", "30"),
                new XAttribute("path", "100,0,100,50"),
                new XAttribute("moveSpeed", "70")));

            object? available = SpinPreviewConverters.Available.Convert(
                star,
                typeof(bool),
                parameter: null,
                CultureInfo.InvariantCulture);

            Assert.True(available is true);
        }

        /// <summary>Electro timing can be previewed even without spin or orbit mover attributes.</summary>
        [Fact]
        public void ElectroTimingHasAnimationPreviewAvailable()
        {
            LevelObject electro = new(new XElement("electro",
                new XAttribute("x", "20"),
                new XAttribute("y", "30"),
                new XAttribute("initialDelay", "0"),
                new XAttribute("offTime", "2"),
                new XAttribute("onTime", "1")));

            object? available = SpinPreviewConverters.Available.Convert(
                electro,
                typeof(bool),
                parameter: null,
                CultureInfo.InvariantCulture);

            Assert.True(available is true);
        }
    }
}

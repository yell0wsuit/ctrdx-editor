using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    public class EditorLayerSelectionTests
    {
        private const string ThreeLayers = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="a"><candy x="1" y="2" /></layer>
            <layer name="b"><star x="3" y="4" timeout="-1" /></layer>
            <layer name="c"><star x="5" y="6" timeout="-1" /></layer>
        </map>
        """;

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(ThreeLayers);
            return vm;
        }

        [Fact]
        public void SetLayerSelection_selects_layers_and_sets_primary_active()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]);

            Assert.Equal(new[] { "a", "c" }, vm.SelectedLayers.Select(l => l.Name));
            Assert.Equal("c", vm.ActiveLayer!.Name);
        }

        [Fact]
        public void SelectingLayers_clears_object_selection()
        {
            EditorViewModel vm = Create();
            vm.SetObjectSelection([vm.Layers[0].Objects[0]]);
            Assert.Equal(1, vm.Selection.Count);

            vm.SetLayerSelection([vm.Layers[1]], vm.Layers[1]);

            Assert.Equal(0, vm.Selection.Count);
            Assert.Single(vm.SelectedLayers);
        }

        [Fact]
        public void SelectingObjects_clears_layer_selection()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);
            Assert.Equal(2, vm.SelectedLayers.Count);

            vm.SetObjectSelection([vm.Layers[2].Objects[0]]);

            Assert.Empty(vm.SelectedLayers);
            Assert.Equal(1, vm.Selection.Count);
        }

        [Fact]
        public void EffectiveLayerTargets_falls_back_to_active_layer_when_no_multi_selection()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[1];

            Assert.Equal(new[] { "b" }, vm.EffectiveLayerTargets.Select(l => l.Name));

            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]);
            Assert.Equal(new[] { "a", "c" }, vm.EffectiveLayerTargets.Select(l => l.Name));
        }
    }
}

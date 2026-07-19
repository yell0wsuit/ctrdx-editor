using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Regressions for the object multi-selection surface.</summary>
    public class EditorMultiSelectRegressionTests
    {
        private const string LayerWithTwoObjects = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="objects"><candy x="1" y="2" /><star x="3" y="4" timeout="-1" /></layer>
        </map>
        """;

        private const string TwoLayers = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="a"><candy x="1" y="2" /></layer>
            <layer name="b"><star x="3" y="4" timeout="-1" /></layer>
        </map>
        """;

        private static EditorViewModel Create(string xml = LayerWithTwoObjects)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(xml);
            return vm;
        }

        // The canvas binds SelectedObject TwoWay, so syncing the canvas after a multi-select writes the
        // primary back into SelectedObject. Setting SelectedObject to the object that is already primary must
        // not discard the rest of the selection - otherwise Ctrl+A collapses to one on the first press.
        /// <inheritdoc/>
        [Fact]
        public void SettingSelectedObjectToCurrentPrimaryKeepsMultiSelection()
        {
            EditorViewModel vm = Create();

            vm.SelectAllInActiveLayer();
            Assert.Equal(2, vm.Selection.Count);

            // Simulate the canvas TwoWay SelectedObject binding writing the primary back.
            vm.SelectedObject = vm.Selection.Primary;

            Assert.Equal(2, vm.Selection.Count);
        }

        // The object panel drives the selection from its selected rows; the last row is primary and its layer
        // becomes active. A selection may span layers.
        /// <inheritdoc/>
        [Fact]
        public void SetObjectSelectionAcrossLayersSelectsAllAndActivatesPrimaryLayer()
        {
            EditorViewModel vm = Create(TwoLayers);
            LevelObject inA = vm.Layers[0].Objects[0];
            LevelObject inB = vm.Layers[1].Objects[0];

            vm.SetObjectSelection([inA, inB]);

            Assert.Equal(2, vm.Selection.Count);
            Assert.Equal(inB, vm.Selection.Primary);
            Assert.Equal("b", vm.ActiveLayer!.Name);
        }

        // Selecting two objects in the same layer selects both.
        /// <inheritdoc/>
        [Fact]
        public void SetObjectSelectionInSameLayerSelectsBoth()
        {
            EditorViewModel vm = Create();
            LevelObject first = vm.Layers[0].Objects[0];
            LevelObject second = vm.Layers[0].Objects[1];

            vm.SetObjectSelection([first, second]);

            Assert.Equal(2, vm.Selection.Count);
        }
    }
}

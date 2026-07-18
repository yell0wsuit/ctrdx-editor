using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the editor's layer-tree, active-layer, and visibility state.</summary>
    public class EditorLayerTreeTests
    {
        private const string TwoLayers = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="a"><candy x="1" y="2" /></layer>
            <layer name="b"><star x="3" y="4" timeout="-1" /></layer>
        </map>
        """;

        /// <summary>Verifies that loading builds one tree row per object layer with object children.</summary>
        [Fact]
        public void LoadBuildsLayerTreeWithObjects()
        {
            EditorViewModel vm = Create();

            Assert.Equal(["a", "b"], vm.Layers.Select(layer => layer.Name));
            Assert.Equal("candy", vm.Layers[0].Objects[0].Type);
        }

        /// <summary>Verifies that the first object layer is active after loading.</summary>
        [Fact]
        public void ActiveLayerDefaultsToFirstObjectLayer()
        {
            EditorViewModel vm = Create();

            Assert.Equal("a", vm.ActiveLayer!.Name);
            Assert.True(vm.Layers[0].IsActive);
        }

        /// <summary>Verifies that placement appends to the active layer.</summary>
        [Fact]
        public void PlaceObjectGoesIntoActiveLayer()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[1];

            _ = vm.PlaceObject("bubble", 100, 100);

            Assert.Contains(vm.Layers[1].Objects, obj => obj.Type == "bubble");
            Assert.DoesNotContain(vm.Layers[0].Objects, obj => obj.Type == "bubble");
        }

        /// <summary>Verifies that hiding a layer effectively hides all of its objects.</summary>
        [Fact]
        public void HidingLayerMarksItsObjectsEffectivelyHidden()
        {
            EditorViewModel vm = Create();

            vm.SetLayerHidden(vm.Layers[0].Layer, true);

            Assert.Contains(vm.Layers[0].Objects[0], vm.EffectivelyHiddenObjects);
        }

        /// <summary>Verifies that individual object visibility contributes to effective visibility.</summary>
        [Fact]
        public void HidingObjectMarksOnlyThatObjectEffectivelyHidden()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];

            vm.SetObjectHidden(candy, true);

            Assert.Contains(candy, vm.EffectivelyHiddenObjects);
            Assert.DoesNotContain(vm.Layers[1].Objects[0], vm.EffectivelyHiddenObjects);
        }

        /// <summary>Publishes a new hidden-set instance so the canvas binding invalidates its styled property.</summary>
        [Fact]
        public void VisibilityChangePublishesNewHiddenSetInstance()
        {
            EditorViewModel vm = Create();
            IReadOnlySet<LevelObject> before = vm.EffectivelyHiddenObjects;

            vm.SetObjectHidden(vm.Layers[0].Objects[0], true);

            Assert.NotSame(before, vm.EffectivelyHiddenObjects);
        }

        /// <summary>Verifies that adding a layer makes the unique new row active.</summary>
        [Fact]
        public void AddLayerCreatesUniqueActiveLayer()
        {
            EditorViewModel vm = Create();

            vm.AddLayer();

            Assert.Equal("Layer", vm.ActiveLayer!.Name);
            Assert.Equal(["a", "b", "Layer"], vm.Layers.Select(layer => layer.Name));
        }

        /// <summary>Verifies valid renames and duplicate-name rejection.</summary>
        [Fact]
        public void RenameLayerRequiresUniqueNonblankName()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[1];

            Assert.False(vm.RenameLayer(vm.Layers[1].Layer, "a"));
            Assert.True(vm.RenameLayer(vm.Layers[1].Layer, "renamed"));
            Assert.Equal("renamed", vm.Layers[1].Name);
            Assert.Equal("renamed", vm.ActiveLayer.Name);
        }

        /// <summary>Verifies that moving the active layer preserves its active identity.</summary>
        [Fact]
        public void MoveActiveLayerReordersRowsAndPreservesActiveLayer()
        {
            EditorViewModel vm = Create();
            vm.SelectedTreeItem = vm.Layers[0];

            vm.MoveActiveLayer(1);

            Assert.Equal(["b", "a"], vm.Layers.Select(layer => layer.Name));
            Assert.Equal("a", vm.ActiveLayer!.Name);
            Assert.Same(vm.ActiveLayer, vm.SelectedTreeItem);
        }

        /// <summary>Moving a specific layer to an index reorders without requiring it to be active.</summary>
        [Fact]
        public void MoveLayerToIndexReordersSpecificLayer()
        {
            EditorViewModel vm = Create();

            vm.MoveLayerToIndex(vm.Layers[0].Layer, 1);

            Assert.Equal(["b", "a"], vm.Layers.Select(layer => layer.Name));
        }

        /// <summary>Deleting a specific layer removes it even when another layer is active.</summary>
        [Fact]
        public void DeleteLayerRemovesSpecificLayer()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[1];

            vm.DeleteLayer(vm.Layers[0].Layer);

            Assert.Equal(["b"], vm.Layers.Select(layer => layer.Name));
        }

        /// <summary>Verifies that moving an object reparents it and preserves selection.</summary>
        [Fact]
        public void MoveObjectToLayerReparentsSelectedObject()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.SelectedObject = candy;

            vm.MoveObjectToLayer(candy, vm.Layers[1].Layer);

            Assert.Empty(vm.Layers[0].Objects);
            Assert.Contains(candy, vm.Layers[1].Objects);
            Assert.Equal(candy, vm.SelectedObject);
            Assert.Equal(candy, vm.SelectedTreeItem);
        }

        /// <summary>Selecting a layer row makes it active without selecting an object.</summary>
        [Fact]
        public void SelectingLayerTreeItemChangesActiveLayer()
        {
            EditorViewModel vm = Create();

            vm.SelectedTreeItem = vm.Layers[1];

            Assert.Same(vm.Layers[1], vm.ActiveLayer);
            Assert.Null(vm.SelectedObject);
        }

        /// <summary>Selecting an object row updates the editor's object selection.</summary>
        [Fact]
        public void SelectingObjectTreeItemChangesSelectedObject()
        {
            EditorViewModel vm = Create();
            LevelObject star = vm.Layers[1].Objects[0];

            vm.SelectedTreeItem = star;

            Assert.Equal(star, vm.SelectedObject);
        }

        /// <summary>Canvas and command selection stays aligned with the highlighted object tree row.</summary>
        [Fact]
        public void SelectedObjectChangesSelectedTreeItem()
        {
            EditorViewModel vm = Create();
            LevelObject star = vm.Layers[1].Objects[0];

            vm.SelectedObject = star;

            Assert.Equal(star, vm.SelectedTreeItem);

            vm.SelectedObject = null;

            Assert.Null(vm.SelectedTreeItem);
        }

        /// <summary>Hiding the locked object releases both lock and selection so visible objects remain interactive.</summary>
        [Fact]
        public void HidingLockedObjectClearsLockAndSelection()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.ToggleLock(candy);

            vm.SetLayerHidden(vm.Layers[0].Layer, true);

            Assert.Null(vm.LockedObject);
            Assert.Null(vm.SelectedObject);
        }

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(TwoLayers);
            return vm;
        }
    }
}

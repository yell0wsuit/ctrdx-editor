using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Verifies multi-layer selection, batch layer operations, and mutual exclusion with object selection.
    /// </summary>
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

        /// <summary>Selecting layer rows records them in tree order and makes the primary row active.</summary>
        [Fact]
        public void SetLayerSelectionSelectsLayersAndSetsPrimaryActive()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]);

            Assert.Equal(["a", "c"], vm.SelectedLayers.Select(l => l.Name));
            Assert.Equal("c", vm.ActiveLayer!.Name);
        }

        /// <summary>Selecting one or more layer rows clears the current object selection.</summary>
        [Fact]
        public void SelectingLayersClearsObjectSelection()
        {
            EditorViewModel vm = Create();
            vm.SetObjectSelection([vm.Layers[0].Objects[0]]);
            Assert.Equal(1, vm.Selection.Count);

            vm.SetLayerSelection([vm.Layers[1]], vm.Layers[1]);

            Assert.Equal(0, vm.Selection.Count);
            _ = Assert.Single(vm.SelectedLayers);
        }

        /// <summary>Selecting an object leaves layer-selection mode and clears the selected layer rows.</summary>
        [Fact]
        public void SelectingObjectsClearsLayerSelection()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);
            Assert.Equal(2, vm.SelectedLayers.Count);

            vm.SetObjectSelection([vm.Layers[2].Objects[0]]);

            Assert.Empty(vm.SelectedLayers);
            Assert.Equal(1, vm.Selection.Count);
        }

        /// <summary>Layer commands target the active layer when no explicit layer-row selection exists.</summary>
        [Fact]
        public void EffectiveLayerTargetsFallsBackToActiveLayerWhenNoMultiSelection()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[1];

            Assert.Equal(["b"], vm.EffectiveLayerTargets.Select(l => l.Name));

            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]);
            Assert.Equal(["a", "c"], vm.EffectiveLayerTargets.Select(l => l.Name));
        }

        /// <summary>Deleting multiple selected layers captures a single undo snapshot.</summary>
        [Fact]
        public void DeleteSelectedLayersRemovesAllInOneUndoStep()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]); // a and c

            vm.DeleteSelectedLayers();

            Assert.Equal(["b"], vm.Layers.Select(l => l.Name));

            vm.Undo();
            Assert.Equal(["a", "b", "c"], vm.Layers.Select(l => l.Name));
        }

        /// <summary>Batch visibility changes apply to every selected layer and no unselected layer.</summary>
        [Fact]
        public void SetSelectedLayersHiddenAppliesToAllTargets()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);

            vm.SetSelectedLayersHidden(true);

            Assert.True(vm.IsLayerHidden(vm.Layers[0].Layer));
            Assert.True(vm.IsLayerHidden(vm.Layers[1].Layer));
            Assert.False(vm.IsLayerHidden(vm.Layers[2].Layer));
        }

        /// <summary>Batch lock changes apply to every selected layer and no unselected layer.</summary>
        [Fact]
        public void SetSelectedLayersLockedAppliesToAllTargets()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);

            vm.SetSelectedLayersLocked(true);

            Assert.True(vm.IsLayerLocked(vm.Layers[0].Layer));
            Assert.True(vm.IsLayerLocked(vm.Layers[1].Layer));
            Assert.False(vm.IsLayerLocked(vm.Layers[2].Layer));
        }

        /// <summary>Moving down shifts each movable noncontiguous target by one position.</summary>
        [Fact]
        public void MoveSelectedLayersDownShiftsNoncontiguousSelectionEachByOne()
        {
            EditorViewModel vm = Create(); // a, b, c at 0,1,2
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[2]], vm.Layers[2]); // a and c

            vm.MoveSelectedLayers(1); // down

            // a: 0->1, c: 2 was last so stays; b moves up past a
            Assert.Equal(["b", "a", "c"], vm.Layers.Select(l => l.Name));
        }

        /// <summary>A selection containing only the bottom layer cannot move farther down.</summary>
        [Fact]
        public void MoveSelectedLayersDownIsBlockedWhenATargetIsAtTheBottom()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[2]], vm.Layers[2]); // c, already bottom

            Assert.False(vm.CanMoveSelectedLayersDown);
        }

        /// <summary>Selected layers can move up when at least one target is below the top row.</summary>
        [Fact]
        public void CanMoveSelectedLayersUpTrueWhenTopmostTargetNotAtTop()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[1], vm.Layers[2]], vm.Layers[2]);

            Assert.True(vm.CanMoveSelectedLayersUp);
        }

        /// <summary>Batch deletion is available when at least one effective target is unlocked.</summary>
        [Fact]
        public void CanDeleteSelectedLayersTrueWhenAnyUnlockedTarget()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0]], vm.Layers[0]);

            Assert.True(vm.CanDeleteSelectedLayers);
        }

        /// <summary>Canvas-style direct object mutations leave layer-selection mode.</summary>
        [Fact]
        public void DirectObjectSelectionNotificationClearsLayerSelection()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);

            vm.Selection.Replace(vm.Layers[2].Objects[0]);
            vm.RaiseSelectedObjectChanged();

            Assert.Empty(vm.SelectedLayers);
            _ = Assert.Single(vm.Selection.Items);
        }

        /// <summary>The compatibility single-object selection surface also leaves layer-selection mode.</summary>
        [Fact]
        public void SelectedObjectSetterClearsLayerSelection()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0]], vm.Layers[0]);

            vm.SelectedObject = vm.Layers[2].Objects[0];

            Assert.Empty(vm.SelectedLayers);
            Assert.NotNull(vm.SelectedObject);
        }

        /// <summary>Tree rebuilds replace selected row wrappers with rows from the rebuilt collection.</summary>
        [Fact]
        public void RefreshObjectListResolvesSelectedLayerRowsByElementIdentity()
        {
            EditorViewModel vm = Create();
            LayerViewModel oldA = vm.Layers[0];
            LayerViewModel oldC = vm.Layers[2];
            vm.SetLayerSelection([oldA, oldC], oldC);

            vm.RefreshObjectList();

            Assert.Equal(["a", "c"], vm.SelectedLayers.Select(layer => layer.Name));
            Assert.Same(vm.Layers[0], vm.SelectedLayers[0]);
            Assert.Same(vm.Layers[2], vm.SelectedLayers[1]);
            Assert.DoesNotContain(oldA, vm.SelectedLayers);
            Assert.DoesNotContain(oldC, vm.SelectedLayers);
        }

        /// <summary>Fallback target bindings are invalidated when the active layer changes.</summary>
        [Fact]
        public void ActiveLayerChangeNotifiesSelectedLayerCapabilities()
        {
            EditorViewModel vm = Create();
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.ActiveLayer = vm.Layers[1];

            Assert.Contains(nameof(EditorViewModel.EffectiveLayerTargets), changed);
            Assert.Contains(nameof(EditorViewModel.CanDeleteSelectedLayers), changed);
            Assert.Contains(nameof(EditorViewModel.CanMoveSelectedLayersUp), changed);
            Assert.Contains(nameof(EditorViewModel.CanMoveSelectedLayersDown), changed);
        }

        /// <summary>Closing a document drops layer rows retained by the previous editor session.</summary>
        [Fact]
        public void CloseLevelClearsLayerSelection()
        {
            EditorViewModel vm = Create();
            vm.SetLayerSelection([vm.Layers[0], vm.Layers[1]], vm.Layers[1]);

            vm.CloseLevel();

            Assert.Empty(vm.SelectedLayers);
        }
    }
}

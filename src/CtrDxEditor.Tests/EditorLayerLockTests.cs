using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Content;
using CtrDxEditor.Converters;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the editor's session-only layer-lock state.</summary>
    public class EditorLayerLockTests
    {
        private const string TwoLayers = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="a"><candy x="1" y="2" /></layer>
            <layer name="b"><star x="3" y="4" timeout="-1" /></layer>
        </map>
        """;

        /// <summary>Locking a layer marks all of its objects effectively locked and flags the row.</summary>
        [Fact]
        public void LockingLayerMarksObjectsAndRow()
        {
            EditorViewModel vm = Create();

            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            Assert.True(vm.Layers[0].IsLocked);
            Assert.Contains(vm.Layers[0].Objects[0], vm.EffectivelyLockedObjects);
            Assert.DoesNotContain(vm.Layers[1].Objects[0], vm.EffectivelyLockedObjects);
        }

        /// <summary>Each lock change publishes a fresh set instance so the canvas binding invalidates.</summary>
        [Fact]
        public void LockChangePublishesNewSetInstance()
        {
            EditorViewModel vm = Create();
            IReadOnlySet<LevelObject> before = vm.EffectivelyLockedObjects;

            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            Assert.NotSame(before, vm.EffectivelyLockedObjects);
        }

        /// <summary>Locking a layer releases selection and pin held on its objects.</summary>
        [Fact]
        public void LockingLayerClearsSelectionAndPinInThatLayer()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.ToggleLock(candy);

            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            Assert.Null(vm.LockedObject);
            Assert.Null(vm.SelectedObject);
        }

        /// <summary>A locked layer's object cannot be selected through the layer tree.</summary>
        [Fact]
        public void LockedLayerObjectCannotBeSelectedFromTree()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            vm.SelectedTreeItem = candy;

            Assert.Null(vm.SelectedObject);
            Assert.Null(vm.SelectedTreeItem);
        }

        /// <summary>Undo cannot restore a selected or pinned object while its layer remains locked.</summary>
        [Fact]
        public void UndoDoesNotRestoreSelectionOrPinInLockedLayer()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.ToggleLock(candy);
            vm.BeginUndoTransaction();
            candy.X = 20;
            vm.CompleteUndoTransaction();
            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            vm.Undo();

            Assert.Null(vm.SelectedObject);
            Assert.Null(vm.SelectedTreeItem);
            Assert.Null(vm.LockedObject);
        }

        /// <summary>Object rows are disabled when their containing layer is locked.</summary>
        [Fact]
        public void LockedLayerDisablesObjectRow()
        {
            EditorViewModel vm = Create();
            LevelObject candy = vm.Layers[0].Objects[0];
            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            object enabled = LockRowEnabledConverter.Instance.Convert(
                [candy, null, vm.EffectivelyLockedObjects],
                typeof(bool),
                null,
                CultureInfo.InvariantCulture);

            Assert.False(Assert.IsType<bool>(enabled));
        }

        /// <summary>A locked layer cannot be deleted or add an undo entry.</summary>
        [Fact]
        public void LockedLayerCannotBeDeleted()
        {
            EditorViewModel vm = Create();
            LevelLayer layer = vm.Layers[0].Layer;
            string? before = vm.ToXml();
            vm.SetLayerLocked(layer, true);

            vm.DeleteLayer(layer);

            Assert.Equal(before, vm.ToXml());
            Assert.Equal(2, vm.Layers.Count);
            Assert.False(vm.CanUndo);
        }

        /// <summary>A locked layer cannot be renamed or add an undo entry.</summary>
        [Fact]
        public void LockedLayerCannotBeRenamed()
        {
            EditorViewModel vm = Create();
            LevelLayer layer = vm.Layers[0].Layer;
            vm.SetLayerLocked(layer, true);

            bool renamed = vm.RenameLayer(layer, "renamed");

            Assert.False(renamed);
            Assert.Equal("a", layer.Name);
            Assert.False(vm.CanUndo);
        }

        /// <summary>A locked layer cannot be shifted or add an undo entry.</summary>
        [Fact]
        public void LockedLayerCannotBeMoved()
        {
            EditorViewModel vm = Create();
            LevelLayer layer = vm.Layers[0].Layer;
            vm.SetLayerLocked(layer, true);

            vm.MoveLayer(layer, 1);

            Assert.Same(layer, vm.Layers[0].Layer);
            Assert.False(vm.CanUndo);
        }

        /// <summary>A locked layer cannot be drag-reordered to another row.</summary>
        [Fact]
        public void LockedLayerCannotBeMovedToIndex()
        {
            EditorViewModel vm = Create();
            LevelLayer layer = vm.Layers[0].Layer;
            vm.SetLayerLocked(layer, true);

            vm.MoveLayerToIndex(layer, 1);

            Assert.Same(layer, vm.Layers[0].Layer);
            Assert.False(vm.CanUndo);
        }

        /// <summary>Locking the active layer immediately disables both reorder directions.</summary>
        [Fact]
        public void LockingActiveLayerDisablesMoveCapabilities()
        {
            EditorViewModel vm = Create();
            vm.ActiveLayer = vm.Layers[0];
            Assert.True(vm.CanMoveActiveLayerDown);

            vm.SetLayerLocked(vm.ActiveLayer.Layer, true);

            Assert.False(vm.CanMoveActiveLayerUp);
            Assert.False(vm.CanMoveActiveLayerDown);
        }

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(TwoLayers);
            return vm;
        }
    }
}

using System.Collections.Generic;

using CtrDxEditor.Content;
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

        /// <summary>Renaming a locked layer keeps it locked under the new name.</summary>
        [Fact]
        public void RenamePreservesLock()
        {
            EditorViewModel vm = Create();
            vm.SetLayerLocked(vm.Layers[0].Layer, true);

            _ = vm.RenameLayer(vm.Layers[0].Layer, "renamed");

            Assert.True(vm.IsLayerLocked(vm.Layers[0].Layer));
            Assert.True(vm.Layers[0].IsLocked);
        }

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(TwoLayers);
            return vm;
        }
    }
}

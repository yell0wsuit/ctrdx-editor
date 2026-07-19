using System.Collections.Generic;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests multi-object editor selection behavior.</summary>
    public class EditorSelectionTests
    {
        // Two layers: layer 0 has objects a,b; layer 1 has object c.
        private static (LevelDocument doc, LevelObject a, LevelObject b, LevelObject c) Build()
        {
            LevelDocument doc = LevelDocument.Parse(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/><star x=\"2\" y=\"2\"/></layer>" +
                "<layer name=\"L1\"><star x=\"3\" y=\"3\"/></layer></map>");
            IReadOnlyList<LevelObject> objs = doc.AllObjects;
            return (doc, objs[0], objs[1], objs[2]);
        }

        /// <summary>Verifies replacement selects one object and makes it primary.</summary>
        [Fact]
        public void ReplaceSelectsSingleObjectAsPrimary()
        {
            (LevelDocument doc, LevelObject a, _, _) = Build();
            EditorSelection selection = new(doc);
            selection.Replace(a);
            Assert.Collection(selection.Items, item => Assert.Same(a, item));
            Assert.Equal(a, selection.Primary);
            Assert.Equal(1, selection.Count);
        }

        /// <summary>Verifies toggling adds and removes objects in the same layer.</summary>
        [Fact]
        public void ToggleSameLayerAddsAndRemoves()
        {
            (LevelDocument doc, LevelObject a, LevelObject b, _) = Build();
            EditorSelection selection = new(doc);
            selection.Replace(a);
            selection.Toggle(b);
            Assert.Collection(selection.Items, item => Assert.Same(a, item), item => Assert.Same(b, item));
            Assert.Equal(b, selection.Primary);
            selection.Toggle(b);
            Assert.Collection(selection.Items, item => Assert.Same(a, item));
            Assert.Equal(a, selection.Primary);
        }

        /// <summary>Verifies toggling an object in another layer adds it (a selection may span layers).</summary>
        [Fact]
        public void ToggleIntoOtherLayerAdds()
        {
            (LevelDocument doc, LevelObject a, LevelObject b, LevelObject c) = Build();
            EditorSelection selection = new(doc);
            selection.Replace(a);
            selection.Toggle(b);
            selection.Toggle(c);
            Assert.Collection(
                selection.Items,
                item => Assert.Same(a, item),
                item => Assert.Same(b, item),
                item => Assert.Same(c, item));
            Assert.Equal(c, selection.Primary);
        }

        /// <summary>Verifies removing the primary object promotes the last remaining object.</summary>
        [Fact]
        public void RemovingPrimaryPromotesLastRemaining()
        {
            (LevelDocument doc, LevelObject a, LevelObject b, _) = Build();
            EditorSelection selection = new(doc);
            selection.Replace(a);
            selection.Toggle(b);
            selection.Toggle(b);
            Assert.Equal(a, selection.Primary);
        }

        /// <summary>Verifies clearing removes every selected object and layer reference.</summary>
        [Fact]
        public void ClearEmptiesSelection()
        {
            (LevelDocument doc, LevelObject a, _, _) = Build();
            EditorSelection selection = new(doc);
            selection.Replace(a);
            selection.Clear();
            Assert.Empty(selection.Items);
            Assert.Null(selection.Primary);
            Assert.Null(selection.Layer);
        }

        /// <summary>Verifies range selection uses the supplied primary object.</summary>
        [Fact]
        public void SetRangeSelectsAllWithGivenPrimary()
        {
            (LevelDocument doc, LevelObject a, LevelObject b, _) = Build();
            EditorSelection selection = new(doc);
            selection.SetRange([a, b], b);
            Assert.Collection(selection.Items, item => Assert.Same(a, item), item => Assert.Same(b, item));
            Assert.Equal(b, selection.Primary);
        }

        /// <summary>Verifies selection mutations raise the changed event.</summary>
        [Fact]
        public void ChangedFiresOnMutation()
        {
            (LevelDocument doc, LevelObject a, _, _) = Build();
            EditorSelection selection = new(doc);
            int fired = 0;
            selection.Changed += () => fired++;
            selection.Replace(a);
            selection.Clear();
            Assert.Equal(2, fired);
        }
    }

    /// <summary>Tests the view model compatibility surface backed by editor selection.</summary>
    public class EditorViewModelSelectionShimTests
    {
        /// <summary>Verifies the selected-object setter updates the selection primary.</summary>
        [Fact]
        public void SelectedObjectSetterUpdatesSelectionPrimary()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/></layer></map>");
            LevelObject obj = vm.Document!.AllObjects[0];

            vm.SelectedObject = obj;

            Assert.Equal(obj, vm.Selection.Primary);
            Assert.Equal(obj, vm.SelectedObject);

            vm.SelectedObject = null;
            Assert.Null(vm.Selection.Primary);
        }

        /// <summary>Verifies select-all spans every unlocked layer in document order.</summary>
        [Fact]
        public void SelectAllObjectsSelectsObjectsAcrossUnlockedLayers()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/><star x=\"2\" y=\"2\"/></layer>" +
                "<layer name=\"L1\"><star x=\"3\" y=\"3\"/></layer>" +
                "<layer name=\"L2\"><bubble x=\"4\" y=\"4\"/></layer></map>");
            vm.ActiveLayer = vm.Layers[0];
            vm.SetLayerLocked(vm.Layers[1].Layer, true);
            vm.SetObjectHidden(vm.Layers[2].Objects[0], true);

            vm.SelectAllObjects();

            Assert.Collection(
                vm.Selection.Items,
                selected => Assert.Same(vm.Layers[0].Objects[0].Element, selected.Element),
                selected => Assert.Same(vm.Layers[0].Objects[1].Element, selected.Element),
                selected => Assert.Same(vm.Layers[2].Objects[0].Element, selected.Element));
            Assert.DoesNotContain(
                vm.Selection.Items,
                selected => ReferenceEquals(vm.Layers[1].Objects[0].Element, selected.Element));
        }

        /// <summary>Verifies document-wide select-all preserves the layer used for placement and paste.</summary>
        [Fact]
        public void SelectAllObjectsKeepsActiveLayer()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/></layer>" +
                "<layer name=\"L1\"><star x=\"2\" y=\"2\"/></layer></map>");
            LayerViewModel active = vm.Layers[0];
            vm.ActiveLayer = active;

            vm.SelectAllObjects();

            Assert.Same(active, vm.ActiveLayer);
        }
    }
}

using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    public class EditorSelectionTests
    {
        // Two layers: layer 0 has objects a,b; layer 1 has object c.
        private static (LevelDocument doc, LevelObject a, LevelObject b, LevelObject c) Build()
        {
            LevelDocument doc = LevelDocument.Parse(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/><star x=\"2\" y=\"2\"/></layer>" +
                "<layer name=\"L1\"><star x=\"3\" y=\"3\"/></layer></map>");
            var objs = doc.AllObjects;
            return (doc, objs[0], objs[1], objs[2]);
        }

        [Fact]
        public void Replace_selects_single_object_as_primary()
        {
            var (doc, a, _, _) = Build();
            var sel = new EditorSelection(doc);
            sel.Replace(a);
            Assert.Equal(new[] { a }, sel.Items);
            Assert.Equal(a, sel.Primary);
            Assert.Equal(1, sel.Count);
        }

        [Fact]
        public void Toggle_same_layer_adds_and_removes()
        {
            var (doc, a, b, _) = Build();
            var sel = new EditorSelection(doc);
            sel.Replace(a);
            sel.Toggle(b);
            Assert.Equal(new[] { a, b }, sel.Items);
            Assert.Equal(b, sel.Primary);
            sel.Toggle(b);
            Assert.Equal(new[] { a }, sel.Items);
            Assert.Equal(a, sel.Primary);
        }

        [Fact]
        public void Toggle_into_other_layer_replaces()
        {
            var (doc, a, b, c) = Build();
            var sel = new EditorSelection(doc);
            sel.Replace(a);
            sel.Toggle(b);
            sel.Toggle(c);
            Assert.Equal(new[] { c }, sel.Items);
            Assert.Equal(c, sel.Primary);
        }

        [Fact]
        public void Removing_primary_promotes_last_remaining()
        {
            var (doc, a, b, _) = Build();
            var sel = new EditorSelection(doc);
            sel.Replace(a);
            sel.Toggle(b);
            sel.Toggle(b);
            Assert.Equal(a, sel.Primary);
        }

        [Fact]
        public void Clear_empties_selection()
        {
            var (doc, a, _, _) = Build();
            var sel = new EditorSelection(doc);
            sel.Replace(a);
            sel.Clear();
            Assert.Empty(sel.Items);
            Assert.Null(sel.Primary);
            Assert.Null(sel.Layer);
        }

        [Fact]
        public void SetRange_selects_all_with_given_primary()
        {
            var (doc, a, b, _) = Build();
            var sel = new EditorSelection(doc);
            sel.SetRange(new[] { a, b }, b);
            Assert.Equal(new[] { a, b }, sel.Items);
            Assert.Equal(b, sel.Primary);
        }

        [Fact]
        public void Changed_fires_on_mutation()
        {
            var (doc, a, _, _) = Build();
            var sel = new EditorSelection(doc);
            int fired = 0;
            sel.Changed += () => fired++;
            sel.Replace(a);
            sel.Clear();
            Assert.Equal(2, fired);
        }
    }

    public class EditorViewModelSelectionShimTests
    {
        [Fact]
        public void SelectedObject_setter_updates_Selection_primary()
        {
            var vm = new EditorViewModel(new SpriteCache(new EmptyContentStore()));
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

        [Fact]
        public void SelectAllInActiveLayer_selects_every_object_in_active_layer()
        {
            var vm = new EditorViewModel(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\"><bubble x=\"1\" y=\"1\"/><star x=\"2\" y=\"2\"/></layer>" +
                "<layer name=\"L1\"><star x=\"3\" y=\"3\"/></layer></map>");
            vm.ActiveLayer = vm.Layers[0];

            vm.SelectAllInActiveLayer();

            Assert.Equal(2, vm.Selection.Count);
            Assert.All(vm.Selection.Items, o => Assert.Same(vm.ActiveLayer!.Layer.Element, o.Element.Parent));
        }
    }
}

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    public class EditorClipboardTests
    {
        private static EditorViewModel Load(string layerBody)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\">" + layerBody + "</layer></map>");
            vm.ActiveLayer = vm.Layers[0];
            return vm;
        }

        [Fact]
        public void DuplicateSelection_clones_selected_with_offset_and_selects_clones()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            LevelObject original = vm.Document!.AllObjects[0];
            vm.Selection.Replace(original);

            vm.DuplicateSelection(16, 16);

            Assert.Equal(2, vm.Document.AllObjects.Count);
            Assert.Equal(1, vm.Selection.Count);
            LevelObject clone = vm.Selection.Primary!;
            Assert.NotSame(original.Element, clone.Element);
            Assert.Equal("26", clone.GetAttr("x"));
            Assert.Equal("36", clone.GetAttr("y"));
        }
    }
}

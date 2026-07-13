using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>The tutorial properties panel exposes icon and text editing controls.</summary>
    public class TutorialFieldBuilderTests
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

        private static ObservableCollection<AttributeFieldViewModel> Build(LevelObject obj)
        {
            ObservableCollection<AttributeFieldViewModel> fields = [];
            TutorialFieldBuilder.Build(fields, obj, new SpriteCache(new EmptyStore()), () => { }, () => { }, () => { });
            return fields;
        }

        /// <summary>Builds an eleven-choice icon picker and an angle field for tutorial icons.</summary>
        [Fact]
        public void IconPanelHasIconPickerAndAngle()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("angle", "0")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel icon = fields.Single(f => f.Name == "icon");
            Assert.Equal(11, icon.EnumOptions!.Length);
            Assert.Contains(fields, f => f.Name == "angle");
        }

        /// <summary>Renames the underlying element when a different tutorial icon is selected.</summary>
        [Fact]
        public void SelectingIconRenamesElement()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel icon = fields.Single(f => f.Name == "icon");
            icon.SelectedOption = icon.EnumOptions!.First(option => option.Value == "tutorial07");
            Assert.Equal("tutorial07", obj.Type);
        }

        /// <summary>A fixed-width (manual) tutorial text shows text, the auto-width toggle, and width.</summary>
        [Fact]
        public void ManualTextPanelHasTextAutoAndWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi"),
                new XAttribute("width", "140")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "autoWidth");
            Assert.Contains(fields, f => f.Name == "width");
        }

        /// <summary>An auto-width tutorial text hides the manual width field.</summary>
        [Fact]
        public void AutoTextPanelHidesWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi")));
            TutorialObject.SetAutoWidth(obj, true);
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "autoWidth");
            Assert.DoesNotContain(fields, f => f.Name == "width");
        }

        /// <summary>Uses the property field and F2 overlay without exposing a redundant panel command.</summary>
        [Fact]
        public void ViewModelHasNoTutorialEditButtonCommand()
        {
            Assert.Null(typeof(EditorViewModel).GetProperty("CanEditTutorialText"));
            Assert.Null(typeof(EditorViewModel).GetProperty("EditTutorialTextCommand"));
            Assert.Null(typeof(EditorViewModel).GetEvent("TutorialTextEditRequested"));
        }

        /// <summary>Shows manual width after a canvas resize disables auto-width.</summary>
        [Fact]
        public void RefreshFieldsShowsWidthAfterCanvasResize()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject text = vm.PlaceObject("tutorialText", 20, 30)!;
            Assert.DoesNotContain(vm.Fields, f => f.Name == "width");

            TutorialTextResize.ApplyDrag(text, 120);
            vm.RefreshFieldValues();

            Assert.Contains(vm.Fields, f => f.Name == "width");
        }
    }
}

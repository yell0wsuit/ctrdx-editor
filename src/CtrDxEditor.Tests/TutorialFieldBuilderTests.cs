using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>The tutorial properties panel exposes icon and text editing controls.</summary>
    public class TutorialFieldBuilderTests
    {
        private static ObservableCollection<AttributeFieldViewModel> Build(LevelObject obj)
        {
            ObservableCollection<AttributeFieldViewModel> fields = [];
            TutorialFieldBuilder.Build(fields, obj, () => { }, () => { }, () => { });
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

        /// <summary>Builds literal text and wrap-width fields for tutorial text.</summary>
        [Fact]
        public void TextPanelHasTextAndWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi"),
                new XAttribute("width", "140")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "width");
        }
    }
}

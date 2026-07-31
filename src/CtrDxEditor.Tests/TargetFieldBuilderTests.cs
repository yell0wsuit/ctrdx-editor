using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the Om Nom skin picker in the properties panel.</summary>
    public class TargetFieldBuilderTests
    {
        private static (AttributeFieldViewModel Field, LevelObject Target) Build(string? targetType = null)
        {
            XElement element = new("target", new XAttribute("x", "10"), new XAttribute("y", "20"));
            if (targetType is not null)
            {
                element.SetAttributeValue("targetType", targetType);
            }

            LevelObject target = new(element);
            ObservableCollection<AttributeFieldViewModel> fields = [];
            TargetFieldBuilder.Build(fields, target, () => { }, () => { });
            return (fields.Single(), target);
        }

        /// <summary>The picker offers Player's choice plus every skin the game can resolve, each named.</summary>
        [Fact]
        public void OffersPlayerChoiceAndEverySkinByName()
        {
            (AttributeFieldViewModel field, _) = Build();

            Assert.Equal("targetType", field.Name);
            Assert.NotNull(field.EnumOptions);
            Assert.Equal(TargetObject.SkinCount + 1, field.EnumOptions.Length);
            Assert.Equal("Player's choice", field.EnumOptions[0].Label);
            Assert.Equal("Classic", field.EnumOptions[1].Label);
            Assert.Equal("Cyborg", field.EnumOptions[^1].Label);
            Assert.DoesNotContain(field.EnumOptions, option => option.Label == option.Value);
        }

        /// <summary>An Om Nom with no targetType shows Player's choice rather than an empty box.</summary>
        [Fact]
        public void TargetWithoutAttributeShowsPlayerChoice()
        {
            (AttributeFieldViewModel field, _) = Build();

            Assert.Equal("Player's choice", field.SelectedOption?.Label);
        }

        /// <summary>
        /// A level's skin choice is shown by name. targetType 8 is skin slot 7, which is the seventh entry
        /// of the game's skin manifest (the classic skin occupies slot 0 and is not in the manifest).
        /// </summary>
        [Fact]
        public void ExistingSkinIsSelected()
        {
            (AttributeFieldViewModel field, _) = Build("8");

            Assert.Equal("Pirate", field.SelectedOption?.Label);
        }

        /// <summary>Picking a skin writes the number the game reads.</summary>
        [Fact]
        public void SelectingSkinWritesTargetType()
        {
            (AttributeFieldViewModel field, LevelObject target) = Build();

            field.SelectedOption = field.EnumOptions!.Single(option => option.Label == "Disco");

            Assert.Equal("12", target.GetAttr("targetType"));
        }

        /// <summary>Going back to Player's choice removes the attribute instead of writing 0.</summary>
        [Fact]
        public void SelectingPlayerChoiceRemovesTargetType()
        {
            (AttributeFieldViewModel field, LevelObject target) = Build("4");

            field.SelectedOption = field.EnumOptions!.Single(option => option.Value == TargetObject.PlayerChoice);

            Assert.Null(target.GetAttr("targetType"));
        }

        /// <summary>
        /// A value the game cannot resolve displays as Player's choice, matching how it loads, but stays in
        /// the XML untouched so a hand-edited level is not rewritten just by being selected.
        /// </summary>
        [Fact]
        public void UnresolvableValueDisplaysAsPlayerChoiceWithoutRewritingXml()
        {
            (AttributeFieldViewModel field, LevelObject target) = Build("99");

            Assert.Equal("Player's choice", field.SelectedOption?.Label);
            Assert.Equal("99", target.GetAttr("targetType"));
        }
    }
}

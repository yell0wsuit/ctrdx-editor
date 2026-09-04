using System;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the optional help-text metadata on attribute fields.</summary>
    public class AttributeFieldHelpTests
    {
        private static LevelObject Obj()
        {
            return new(new XElement("rocket"));
        }

        /// <summary>An attribute with no <c>Attr.&lt;name&gt;.Help</c> string reports no help rather than an empty tooltip.</summary>
        [Fact]
        public void FieldWithoutHelpTextHasNoHelp()
        {
            AttributeFieldViewModel field = new(Obj(), "angle", AttrType.Number, null, () => { });
            Assert.False(field.HasHelp);
            Assert.Null(field.HelpText);
        }

        /// <summary>
        /// A field picks up its help from the conventional <c>Attr.&lt;name&gt;.Help</c> string, so adding
        /// the localization entry alone is enough to give the panel its hint icon.
        /// </summary>
        [Fact]
        public void FieldResolvesHelpTextByConvention()
        {
            AttributeFieldViewModel field = new(Obj(), "impulse", AttrType.Number, null, () => { });
            Assert.True(field.HasHelp);
            Assert.StartsWith("Thrust strength", field.HelpText, StringComparison.Ordinal);
        }

        /// <summary>An explicit assignment still wins, because object initializers run after the constructor.</summary>
        [Fact]
        public void ExplicitHelpTextOverridesTheConvention()
        {
            AttributeFieldViewModel field = new(Obj(), "impulse", AttrType.Number, null, () => { })
            {
                HelpText = "Thrust strength.",
            };
            Assert.True(field.HasHelp);
            Assert.Equal("Thrust strength.", field.HelpText);
        }

        /// <summary>The generic group attribute does not inherit tutorial-specific first-trigger guidance.</summary>
        [Fact]
        public void GenericGroupFieldHasNoTutorialHelp()
        {
            AttributeFieldViewModel field = new(Obj(), "group", AttrType.Whole, null, () => { });

            Assert.False(field.HasHelp);
            Assert.Null(field.HelpText);
        }

        /// <summary>The <c>time</c> field clamps to 1 so the spinner cannot reach 0, which the game would read as an instant burnout.</summary>
        [Fact]
        public void TimeFieldMinimumIsOne()
        {
            AttributeFieldViewModel field = new(Obj(), "time", AttrType.Number, null, () => { });
            Assert.Equal(1, field.NumericMinimum);
        }

        /// <summary>Tutorial motion help reflects DX's separate visibility and path timelines.</summary>
        [Fact]
        public void TutorialMotionHelpDoesNotClaimLoopingIgnoresFades()
        {
            AttributeOptionViewModel[] options = [new("looping", "Looping")];
            AttributeFieldViewModel field = new("motion", options, () => "looping", _ => { }, () => { });

            Assert.True(field.HasHelp);
            Assert.Contains("visibility", field.HelpText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ignores the fade", field.HelpText, StringComparison.OrdinalIgnoreCase);
        }
    }
}

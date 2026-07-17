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

        /// <summary>Help text is opt-in, so a field left without it reports no help rather than an empty tooltip.</summary>
        [Fact]
        public void FieldWithoutHelpTextHasNoHelp()
        {
            AttributeFieldViewModel field = new(Obj(), "impulse", AttrType.Number, null, () => { });
            Assert.False(field.HasHelp);
            Assert.Null(field.HelpText);
        }

        /// <summary>Setting help text flips <see cref="AttributeFieldViewModel.HasHelp"/>, which is what shows the hint icon in the panel.</summary>
        [Fact]
        public void FieldWithHelpTextReportsHasHelp()
        {
            AttributeFieldViewModel field = new(Obj(), "impulse", AttrType.Number, null, () => { })
            {
                HelpText = "Thrust strength.",
            };
            Assert.True(field.HasHelp);
            Assert.Equal("Thrust strength.", field.HelpText);
        }

        /// <summary>The <c>time</c> field clamps to 1 so the spinner cannot reach 0, which the game would read as an instant burnout.</summary>
        [Fact]
        public void TimeFieldMinimumIsOne()
        {
            AttributeFieldViewModel field = new(Obj(), "time", AttrType.Number, null, () => { });
            Assert.Equal(1, field.NumericMinimum);
        }
    }
}

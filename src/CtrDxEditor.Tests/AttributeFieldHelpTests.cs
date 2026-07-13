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
        private static LevelObject Obj() => new(new XElement("rocket"));

        [Fact]
        public void FieldWithoutHelpTextHasNoHelp()
        {
            var field = new AttributeFieldViewModel(Obj(), "impulse", AttrType.Number, null, () => { });
            Assert.False(field.HasHelp);
            Assert.Null(field.HelpText);
        }

        [Fact]
        public void FieldWithHelpTextReportsHasHelp()
        {
            var field = new AttributeFieldViewModel(Obj(), "impulse", AttrType.Number, null, () => { })
            {
                HelpText = "Thrust strength.",
            };
            Assert.True(field.HasHelp);
            Assert.Equal("Thrust strength.", field.HelpText);
        }

        [Fact]
        public void TimeFieldMinimumIsOne()
        {
            var field = new AttributeFieldViewModel(Obj(), "time", AttrType.Number, null, () => { });
            Assert.Equal(1, field.NumericMinimum);
        }
    }
}

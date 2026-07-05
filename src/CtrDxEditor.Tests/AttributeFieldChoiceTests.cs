using System.Linq;

using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the delegate-backed choice field used by grab binding.</summary>
    public class AttributeFieldChoiceTests
    {
        /// <summary>Delegate-backed fields read and write through the supplied accessors.</summary>
        [Fact]
        public void ChoiceFieldReadsAndWritesThroughDelegates()
        {
            string state = "a";
            AttributeOptionViewModel[] options =
            [
                new("a", "Option A"),
                new("b", "Option B"),
            ];
            AttributeFieldViewModel field = new(
                "attachTo", options, () => state, v => state = v ?? "", () => { });

            Assert.Equal("Option A", field.SelectedOption!.Label);

            field.SelectedOption = options.Single(o => o.Value == "b");

            Assert.Equal("b", state);
            Assert.Equal("Option B", field.SelectedOption!.Label);
        }
    }
}

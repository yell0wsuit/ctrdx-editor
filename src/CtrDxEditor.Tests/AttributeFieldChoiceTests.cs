using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;
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

        /// <summary>A descriptor field can localize under a UI-only key while writing its raw XML name.</summary>
        [Fact]
        public void AttributeChoiceSupportsSeparateLocalizationName()
        {
            LevelObject sock = new(XElement.Parse("""<sock group="0" />"""));
            AttributeFieldViewModel field = new(
                sock,
                "group",
                AttrType.Enum,
                ["0", "1", "2"],
                () => { },
                labelName: "sockGroup");

            Assert.Equal("group", field.Name);
            Assert.Equal(Localizer.AttributeName("sockGroup"), field.Label);
            Assert.NotEqual(Localizer.AttributeName("group"), field.Label);
            AttributeOptionViewModel[] options = field.EnumOptions!;
            Assert.Equal(["0", "1", "2"], options.Select(o => o.Value));

            field.SelectedOption = options.Single(o => o.Value == "2");

            Assert.Equal("2", sock.GetAttr("group"));
            Assert.Null(sock.GetAttr("sockGroup"));
        }

        /// <summary>Synthetic bool fields can map checkbox state through arbitrary accessors.</summary>
        [Fact]
        public void SyntheticBoolFieldReadsAndWritesThroughDelegates()
        {
            string state = "-1";
            AttributeFieldViewModel field = new(
                "autoCatch",
                Core.Descriptors.AttrType.Bool,
                () => int.Parse(state, CultureInfo.InvariantCulture) > 0 ? "true" : "false",
                v => state = v == "true" ? "100" : "-1",
                () => { });

            Assert.True(field.IsBool);
            Assert.False(field.BoolValue);

            field.BoolValue = true;
            Assert.Equal("100", state);
        }

        /// <summary>Fields are enabled by default and notify when disabled.</summary>
        [Fact]
        public void IsEnabledDefaultsTrueAndNotifies()
        {
            string state = "0";
            AttributeFieldViewModel field = new(
                "wheel", Core.Descriptors.AttrType.Bool, () => state, v => state = v ?? "", () => { });

            Assert.True(field.IsEnabled);
            bool notified = false;
            field.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(field.IsEnabled))
                {
                    notified = true;
                }
            };
            field.IsEnabled = false;
            Assert.True(notified);
        }

        /// <summary>The magic-hat group spinner is a magnitude and cannot go negative.</summary>
        [Fact]
        public void GroupFieldMinimumIsZero()
        {
            LevelObject sock = new(XElement.Parse("""<sock group="0" />"""));
            AttributeFieldViewModel field = new(sock, "group", AttrType.Whole, null, () => { });

            Assert.Equal(0, field.NumericMinimum);
        }
    }
}

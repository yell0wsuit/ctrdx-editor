using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the conveyor properties-panel fields.</summary>
    public class ConveyorFieldBuilderTests
    {
        private static (ObservableCollection<AttributeFieldViewModel>, LevelObject) Build(params (string, string)[] attrs)
        {
            XElement e = new("transporter");
            foreach ((string k, string v) in attrs)
            {
                e.SetAttributeValue(k, v);
            }
            LevelObject belt = new(e);
            ObservableCollection<AttributeFieldViewModel> fields = [];
            ConveyorFieldBuilder.Build(fields, belt, () => { }, () => { }, () => { });
            return (fields, belt);
        }

        /// <summary>Geometry comes first; automatic controls sit at the bottom.</summary>
        [Fact]
        public void BuildsGeometryThenAutomaticControls()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("velocity", "10"));
            string[] names = [.. fields.Select(f => f.Name)];
            Assert.Equal(["length", "width", "angle", "auto", "velocity", "direction"], names);
        }

        /// <summary>Manual conveyors hide velocity and direction because the game ignores them.</summary>
        [Fact]
        public void ManualConveyorHidesAutomaticControls()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("type", "manual"), ("velocity", "10"));
            string[] names = [.. fields.Select(f => f.Name)];
            Assert.Equal(["length", "width", "angle", "auto"], names);
        }

        /// <summary>Toggling Automatic requests a field rebuild so conditional controls update immediately.</summary>
        [Fact]
        public void TogglingAutomaticRebuildsFields()
        {
            LevelObject belt = new(new XElement("transporter"));
            ObservableCollection<AttributeFieldViewModel> fields = [];
            int rebuilds = 0;
            ConveyorFieldBuilder.Build(fields, belt, () => { }, () => { }, () => rebuilds++);

            fields.Single(f => f.Name == "auto").BoolValue = false;

            Assert.Equal(1, rebuilds);
        }

        /// <summary>The auto checkbox is checked when the belt has no type attribute.</summary>
        [Fact]
        public void AutoFieldReflectsAbsentType()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("velocity", "10"));
            AttributeFieldViewModel auto = fields.Single(f => f.Name == "auto");
            Assert.True(auto.BoolValue);
        }

        /// <summary>Unchecking auto writes type="manual".</summary>
        [Fact]
        public void UncheckingAutoWritesManual()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject belt) = Build(("velocity", "10"));
            AttributeFieldViewModel auto = fields.Single(f => f.Name == "auto");
            auto.BoolValue = false;
            Assert.Equal("manual", belt.GetAttr("type"));
        }

        /// <summary>Checking auto removes the type attribute.</summary>
        [Fact]
        public void CheckingAutoRemovesType()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject belt) = Build(("type", "manual"));
            AttributeFieldViewModel auto = fields.Single(f => f.Name == "auto");
            Assert.False(auto.BoolValue);
            auto.BoolValue = true;
            Assert.Null(belt.GetAttr("type"));
        }
    }
}

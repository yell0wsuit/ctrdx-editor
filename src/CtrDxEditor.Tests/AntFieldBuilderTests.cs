using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests ant-conveyor semantic property fields.</summary>
    public class AntFieldBuilderTests
    {
        /// <summary>The Closed loop checkbox maps directly to a terminal anchor offset.</summary>
        [Fact]
        public void ClosedLoopFieldMapsToTerminalAnchor()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject ants, _, _) = Build("100,0");
            AttributeFieldViewModel closed = fields.Single(f => f.Name == "closedLoop");

            closed.BoolValue = true;
            Assert.Equal("100,0,0,0", ants.GetAttr("path"));

            closed.BoolValue = false;
            Assert.Equal("100,0", ants.GetAttr("path"));
        }

        /// <summary>Path geometry stays canvas-only while semantic controls remain in the property panel.</summary>
        [Fact]
        public void FieldsExposeSpeedClosureAndDirection()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _, _, _) = Build("100,0");

            Assert.Equal(["moveSpeed", "closedLoop", "polylineClockwise"], fields.Select(f => f.Name));
            Assert.True(fields[0].IsNumeric);
            Assert.True(fields[1].IsBool);
            Assert.True(fields[2].IsBool);
        }

        /// <summary>Direction is editable only for a loop and reverses traversal without changing its shape.</summary>
        [Fact]
        public void ClockwiseFieldReversesClosedPathOrder()
        {
            (ObservableCollection<AttributeFieldViewModel> openFields, _, _, _) = Build("100,0,100,100");
            Assert.False(openFields.Single(f => f.Name == "polylineClockwise").IsEnabled);
            (ObservableCollection<AttributeFieldViewModel> lineLoopFields, _, _, _) = Build("100,0,0,0");
            Assert.False(lineLoopFields.Single(f => f.Name == "polylineClockwise").IsEnabled);

            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject ants, _, _) =
                Build("100,0,100,100,0,0");
            AttributeFieldViewModel clockwise = fields.Single(f => f.Name == "polylineClockwise");

            Assert.True(clockwise.IsEnabled);
            Assert.True(clockwise.BoolValue);

            clockwise.BoolValue = false;

            Assert.Equal("100,100,100,0,0,0", ants.GetAttr("path"));
            Assert.Equal("100", ants.GetAttr("moveSpeed"));
        }

        /// <summary>Changing closure immediately refreshes whether direction is editable.</summary>
        [Fact]
        public void ClosedLoopFieldRefreshesClockwiseAvailability()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _, _, _) = Build("100,0,100,100");
            AttributeFieldViewModel closed = fields.Single(f => f.Name == "closedLoop");
            AttributeFieldViewModel clockwise = fields.Single(f => f.Name == "polylineClockwise");
            Assert.False(clockwise.IsEnabled);

            closed.BoolValue = true;

            Assert.True(clockwise.IsEnabled);
        }

        /// <summary>Direction owns traversal while speed edits expose and store a positive magnitude.</summary>
        [Fact]
        public void MoveSpeedIsPositiveMagnitudeWithoutChangingImportedDirection()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject ants, _, _) =
                Build("100,0,100,100,0,0", moveSpeed: "-100");
            AttributeFieldViewModel speed = fields.Single(f => f.Name == "moveSpeed");
            AttributeFieldViewModel clockwise = fields.Single(f => f.Name == "polylineClockwise");

            Assert.Equal(1, speed.NumericMinimum);
            Assert.Equal("100", speed.Value);
            Assert.False(clockwise.BoolValue);

            speed.Value = "75";

            Assert.Equal("75", ants.GetAttr("moveSpeed"));
            Assert.Equal("100,100,100,0,0,0", ants.GetAttr("path"));
            Assert.False(clockwise.BoolValue);
        }

        /// <summary>Semantic closure participates in the same changing/changed undo callbacks as attributes.</summary>
        [Fact]
        public void ClosedLoopInvokesUndoCallbacks()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _, Counter changing, Counter changed) = Build("100,0");

            fields.Single(f => f.Name == "closedLoop").BoolValue = true;

            Assert.Equal(1, changing.Value);
            Assert.Equal(1, changed.Value);
        }

        /// <summary>Closing is disabled when the terminal anchor cannot fit in the stored-point limit.</summary>
        [Fact]
        public void ClosedLoopIsDisabledForMaximumCapacityOpenPath()
        {
            string path = string.Join(",", Enumerable.Range(1, 99)
                .SelectMany(i => new[] { i.ToString(CultureInfo.InvariantCulture), "0" }));
            (ObservableCollection<AttributeFieldViewModel> fields, _, _, _) = Build(path);

            Assert.False(fields.Single(f => f.Name == "closedLoop").IsEnabled);
        }

        private static (
            ObservableCollection<AttributeFieldViewModel> Fields,
            LevelObject Ants,
            Counter Changing,
            Counter Changed) Build(string path, string moveSpeed = "100")
        {
            LevelObject ants = new(new XElement(
                "ants",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("path", path),
                new XAttribute("moveSpeed", moveSpeed)));
            ObservableCollection<AttributeFieldViewModel> fields = [];
            Counter changing = new();
            Counter changed = new();

            AntFieldBuilder.Build(fields, ants, () => changed.Value++, () => changing.Value++);

            return (fields, ants, changing, changed);
        }

        private sealed class Counter
        {
            public int Value { get; set; }
        }
    }
}

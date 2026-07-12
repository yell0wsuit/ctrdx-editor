using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests conveyor auto/manual semantics and arrow direction, matching the game.</summary>
    public class ConveyorObjectTests
    {
        private static LevelObject Belt(params (string, string)[] attrs)
        {
            XElement e = new("transporter");
            foreach ((string k, string v) in attrs)
            {
                e.SetAttributeValue(k, v);
            }
            return new LevelObject(e);
        }

        [Fact]
        public void AbsentTypeIsAutomatic()
        {
            Assert.True(ConveyorObject.IsAuto(Belt(("velocity", "10"))));
        }

        [Fact]
        public void ManualTypeIsNotAutomatic()
        {
            Assert.False(ConveyorObject.IsAuto(Belt(("type", "manual"))));
        }

        [Fact]
        public void SetAutoTrueRemovesTypeAttribute()
        {
            LevelObject belt = Belt(("type", "manual"));
            ConveyorObject.SetAuto(belt, true);
            Assert.Null(belt.GetAttr("type"));
            Assert.True(ConveyorObject.IsAuto(belt));
        }

        [Fact]
        public void SetAutoFalseWritesManual()
        {
            LevelObject belt = Belt(("velocity", "10"));
            ConveyorObject.SetAuto(belt, false);
            Assert.Equal("manual", belt.GetAttr("type"));
        }

        [Fact]
        public void ManualBeltHasNoArrow()
        {
            Assert.Equal(0, ConveyorObject.ArrowSign(Belt(("type", "manual"), ("velocity", "10"), ("direction", "forward"))));
        }

        [Fact]
        public void ForwardAutoArrowIsNegative()
        {
            // adjustedVelocity = velocity * (forward ? -1 : 1) => negative => arrow -1 (game ConveyorBelt).
            Assert.Equal(-1, ConveyorObject.ArrowSign(Belt(("velocity", "10"), ("direction", "forward"))));
        }

        [Fact]
        public void BackwardAutoArrowIsPositive()
        {
            Assert.Equal(1, ConveyorObject.ArrowSign(Belt(("velocity", "10"), ("direction", "backward"))));
        }
    }
}

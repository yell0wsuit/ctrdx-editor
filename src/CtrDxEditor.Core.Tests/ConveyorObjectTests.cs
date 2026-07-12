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

        /// <summary>A belt with no type attribute is automatic.</summary>
        [Fact]
        public void AbsentTypeIsAutomatic()
        {
            Assert.True(ConveyorObject.IsAuto(Belt(("velocity", "10"))));
        }

        /// <summary>A belt with type="manual" is not automatic.</summary>
        [Fact]
        public void ManualTypeIsNotAutomatic()
        {
            Assert.False(ConveyorObject.IsAuto(Belt(("type", "manual"))));
        }

        /// <summary>Setting automatic removes the type attribute.</summary>
        [Fact]
        public void SetAutoTrueRemovesTypeAttribute()
        {
            LevelObject belt = Belt(("type", "manual"));
            ConveyorObject.SetAuto(belt, true);
            Assert.Null(belt.GetAttr("type"));
            Assert.True(ConveyorObject.IsAuto(belt));
        }

        /// <summary>Clearing automatic writes type="manual".</summary>
        [Fact]
        public void SetAutoFalseWritesManual()
        {
            LevelObject belt = Belt(("velocity", "10"));
            ConveyorObject.SetAuto(belt, false);
            Assert.Equal("manual", belt.GetAttr("type"));
        }

        /// <summary>A manual belt shows no arrow (sign 0).</summary>
        [Fact]
        public void ManualBeltHasNoArrow()
        {
            Assert.Equal(0, ConveyorObject.ArrowSign(Belt(("type", "manual"), ("velocity", "10"), ("direction", "forward"))));
        }

        /// <summary>A forward automatic belt's arrow points negative (game adjustedVelocity).</summary>
        [Fact]
        public void ForwardAutoArrowIsNegative()
        {
            // adjustedVelocity = velocity * (forward ? -1 : 1) => negative => arrow -1 (game ConveyorBelt).
            Assert.Equal(-1, ConveyorObject.ArrowSign(Belt(("velocity", "10"), ("direction", "forward"))));
        }

        /// <summary>A backward automatic belt's arrow points positive.</summary>
        [Fact]
        public void BackwardAutoArrowIsPositive()
        {
            Assert.Equal(1, ConveyorObject.ArrowSign(Belt(("velocity", "10"), ("direction", "backward"))));
        }
    }
}

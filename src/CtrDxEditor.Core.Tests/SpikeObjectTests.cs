using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for spike XML helpers.</summary>
    public class SpikeObjectTests
    {
        /// <summary>Changing spike size keeps both the XML element name and size attribute aligned.</summary>
        [Theory]
        [InlineData("1", "spike1")]
        [InlineData("2", "spike2")]
        [InlineData("3", "spike3")]
        [InlineData("4", "spike4")]
        public void SettingSizeRenamesSpikeElement(string size, string element)
        {
            LevelObject spike = new(XElement.Parse("""<spike1 x="10" y="20" angle="0" size="1" toggled="false" />"""));

            SpikeObject.SetSize(spike, size);

            Assert.Equal(element, spike.Type);
            Assert.Equal(size, spike.GetAttr("size"));
        }

        /// <summary>Invalid spike sizes are ignored rather than producing unsupported spike elements.</summary>
        [Theory]
        [InlineData("0")]
        [InlineData("5")]
        [InlineData("large")]
        public void SettingInvalidSizeLeavesSpikeUnchanged(string size)
        {
            LevelObject spike = new(XElement.Parse("""<spike2 x="10" y="20" angle="0" size="2" />"""));

            SpikeObject.SetSize(spike, size);

            Assert.Equal("spike2", spike.Type);
            Assert.Equal("2", spike.GetAttr("size"));
        }

        /// <summary>The synthetic toggle checkbox maps game XML values to a boolean editor state.</summary>
        [Theory]
        [InlineData(null, false)]
        [InlineData("false", false)]
        [InlineData("1", true)]
        [InlineData("2", true)]
        public void ToggledCheckboxReadsGameValues(string? toggled, bool expected)
        {
            LevelObject spike = new(new XElement("spike1", new XAttribute("x", "10"), new XAttribute("y", "20")));
            if (toggled is not null)
            {
                spike.SetAttr("toggled", toggled);
            }

            Assert.Equal(expected, SpikeObject.IsToggled(spike));
        }

        /// <summary>Turning a spike toggle on defaults it to group 1; turning it off writes false.</summary>
        [Fact]
        public void SetToggledWritesGroupOrFalse()
        {
            LevelObject spike = new(XElement.Parse("""<spike1 x="10" y="20" angle="0" size="1" toggled="false" />"""));

            SpikeObject.SetToggled(spike, true);
            Assert.Equal("1", spike.GetAttr("toggled"));

            SpikeObject.SetToggled(spike, false);
            Assert.Equal("false", spike.GetAttr("toggled"));
        }

        /// <summary>Rotatable spike sprite keys include the current group so the correct button quad can be drawn.</summary>
        [Theory]
        [InlineData("1", "spike3_toggled_1")]
        [InlineData("2", "spike3_toggled_2")]
        public void SpriteKeyIncludesToggleGroup(string group, string expected)
        {
            LevelObject spike = new(XElement.Parse($"""<spike3 x="10" y="20" angle="0" size="3" toggled="{group}" />"""));

            Assert.Equal(expected, SpikeObject.SpriteKey(spike));
        }

        /// <summary>Spike resize hit-testing finds handles at the rotated strip ends.</summary>
        [Fact]
        public void ResizeHitTestFindsRotatedEndHandle()
        {
            LevelObject spike = new(XElement.Parse("""<spike4 x="100" y="200" angle="90" size="4" toggled="false" />"""));

            SpikeResize.Handle handle = SpikeResize.HitTest(spike, new Core.Geometry.Vec2(100, 200 + (568 / 6.0)), 1, 4, 8);

            Assert.Equal(SpikeResize.Handle.ResizeEnd, handle);
        }

        /// <summary>Dragging a spike end chooses the nearest DX-backed spike size and renames the element.</summary>
        [Fact]
        public void ResizeDragChangesSpikeSize()
        {
            LevelObject spike = new(XElement.Parse("""<spike1 x="100" y="200" angle="0" size="1" toggled="false" />"""));

            SpikeResize.ApplyDrag(spike, new Core.Geometry.Vec2(100 + (568 / 6.0), 200), 1);

            Assert.Equal("spike4", spike.Type);
            Assert.Equal("4", spike.GetAttr("size"));
        }

        /// <summary>Toggled spike resizing uses the rotatable DX widths when choosing the nearest size.</summary>
        [Fact]
        public void ResizeDragUsesToggledWidths()
        {
            LevelObject spike = new(XElement.Parse("""<spike1 x="100" y="200" angle="0" size="1" toggled="1" />"""));

            SpikeResize.ApplyDrag(spike, new Core.Geometry.Vec2(100 + (446 / 6.0), 200), 1);

            Assert.Equal("spike3", spike.Type);
            Assert.Equal("3", spike.GetAttr("size"));
            Assert.Equal("1", spike.GetAttr("toggled"));
        }
    }
}

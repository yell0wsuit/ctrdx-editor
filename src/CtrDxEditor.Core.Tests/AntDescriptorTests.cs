using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests ant-conveyor registration and placement defaults.</summary>
    public class AntDescriptorTests
    {
        /// <summary>The descriptor exposes the exact Experiments XML attributes in authored order.</summary>
        [Fact]
        public void DescriptorMatchesExperimentsXml()
        {
            ObjectDescriptor ants = DescriptorTable.CtrObjects.For(AntPath.Element)!;

            Assert.Equal("Ant conveyor", ants.DisplayName);
            Assert.Equal("Cut the Rope: Experiments", ants.Game);
            Assert.Equal(int.MaxValue, ants.MaxCount);
            Assert.Equal(["path", "moveSpeed"], ants.Attributes.Select(a => a.Name));
        }

        /// <summary>Palette placement creates an immediately visible and editable two-point path.</summary>
        [Fact]
        public void PalettePlacementCreatesEditablePath()
        {
            LevelObject ants = Placement.CreateObject(DescriptorTable.CtrObjects.For(AntPath.Element)!, 20, 30);

            Assert.Equal(AntPath.Element, ants.Type);
            Assert.Equal(20, ants.X);
            Assert.Equal(30, ants.Y);
            Assert.Equal(AntPath.DefaultPath, ants.GetAttr("path"));
            Assert.Equal(AntPath.DefaultMoveSpeed, ants.GetAttr("moveSpeed"));
        }
    }
}

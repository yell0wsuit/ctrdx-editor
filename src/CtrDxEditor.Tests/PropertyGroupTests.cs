using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests grouping of property panel fields into collapsible sections.</summary>
    public class PropertyGroupTests
    {
        private static AttributeFieldViewModel Field(string name, string? header, int index)
        {
            LevelObject obj = new(new XElement("hand"));
            return new AttributeFieldViewModel(obj, name, AttrType.Whole, null, () => { })
            {
                GroupHeader = header,
                GroupIndex = index,
            };
        }

        /// <summary>A field carries no group by default, so existing panels stay ungrouped.</summary>
        [Fact]
        public void FieldsAreUngroupedByDefault()
        {
            LevelObject obj = new(new XElement("hand"));
            AttributeFieldViewModel f = new(obj, "x", AttrType.Whole, null, () => { });

            Assert.Null(f.GroupHeader);
            Assert.Equal(-1, f.GroupIndex);
        }

        /// <summary>A group with no header renders bare rather than as an expander.</summary>
        [Fact]
        public void HeaderlessGroupHasNoHeader()
        {
            PropertyGroupViewModel g = new(null, -1);
            Assert.False(g.HasHeader);
            Assert.True(g.IsExpanded);
        }

        /// <summary>A titled group reports a header and starts expanded.</summary>
        [Fact]
        public void TitledGroupReportsHeader()
        {
            PropertyGroupViewModel g = new("Segment 1", 1);
            Assert.True(g.HasHeader);
            Assert.Equal("Segment 1", g.Header);
            Assert.Equal(1, g.Index);
        }

        /// <summary>Consecutive fields sharing a group land in one section, in order.</summary>
        [Fact]
        public void ConsecutiveFieldsGroupTogether()
        {
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(
            [
                Field("x", null, -1),
                Field("y", null, -1),
                Field("segment1Angle", "Segment 1", 1),
                Field("segment1Length", "Segment 1", 1),
                Field("segment2Angle", "Segment 2", 2),
            ])];

            Assert.Equal(3, groups.Length);
            Assert.False(groups[0].HasHeader);
            Assert.Equal(2, groups[0].Fields.Count);
            Assert.Equal("Segment 1", groups[1].Header);
            Assert.Equal(2, groups[1].Fields.Count);
            Assert.Equal("Segment 2", groups[2].Header);
            _ = Assert.Single(groups[2].Fields);
        }

        /// <summary>Two groups with the same header but different indices stay separate.</summary>
        [Fact]
        public void SameHeaderDifferentIndexStaysSeparate()
        {
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(
            [
                Field("segment1Angle", "Segment", 1),
                Field("segment2Angle", "Segment", 2),
            ])];

            Assert.Equal(2, groups.Length);
        }
    }
}

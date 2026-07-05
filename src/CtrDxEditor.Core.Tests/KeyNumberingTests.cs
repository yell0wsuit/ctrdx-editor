using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests 0-based smallest-unused key assignment.</summary>
    public class KeyNumberingTests
    {
        [Fact]
        public void EmptyYieldsZero()
        {
            Assert.Equal("0", KeyNumbering.NextKey([]));
        }

        [Fact]
        public void FillsSmallestGap()
        {
            Assert.Equal("1", KeyNumbering.NextKey(["0", "2"]));
        }

        [Fact]
        public void AppendsAfterContiguousRun()
        {
            Assert.Equal("3", KeyNumbering.NextKey(["0", "1", "2"]));
        }

        [Fact]
        public void IgnoresNonIntegerAndNullKeys()
        {
            Assert.Equal("0", KeyNumbering.NextKey(["first", null]));
        }
    }
}

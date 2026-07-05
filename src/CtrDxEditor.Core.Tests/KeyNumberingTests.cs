using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests 0-based smallest-unused key assignment.</summary>
    public class KeyNumberingTests
    {
        /// <summary>Empty key sets start at zero.</summary>
        [Fact]
        public void EmptyYieldsZero()
        {
            Assert.Equal("0", KeyNumbering.NextKey([]));
        }

        /// <summary>The next key fills the smallest unused non-negative integer gap.</summary>
        [Fact]
        public void FillsSmallestGap()
        {
            Assert.Equal("1", KeyNumbering.NextKey(["0", "2"]));
        }

        /// <summary>Contiguous existing keys append the next integer.</summary>
        [Fact]
        public void AppendsAfterContiguousRun()
        {
            Assert.Equal("3", KeyNumbering.NextKey(["0", "1", "2"]));
        }

        /// <summary>Null and non-integer keys do not reserve numeric slots.</summary>
        [Fact]
        public void IgnoresNonIntegerAndNullKeys()
        {
            Assert.Equal("0", KeyNumbering.NextKey(["first", null]));
        }
    }
}

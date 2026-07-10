using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests pair-filling group assignment for magic hats.</summary>
    public class SockGroupingTests
    {
        /// <summary>No existing hats start at group zero.</summary>
        [Fact]
        public void EmptyYieldsZero()
        {
            Assert.Equal("0", SockGrouping.NextGroup([]));
        }

        /// <summary>A lone hat's group is reused to complete its pair.</summary>
        [Fact]
        public void CompletesOpenPair()
        {
            Assert.Equal("0", SockGrouping.NextGroup(["0"]));
        }

        /// <summary>A complete pair starts a fresh group.</summary>
        [Fact]
        public void CompletePairStartsFreshGroup()
        {
            Assert.Equal("1", SockGrouping.NextGroup(["0", "0"]));
        }

        /// <summary>The smallest open pair is completed before a larger one.</summary>
        [Fact]
        public void FillsSmallestOpenPairFirst()
        {
            Assert.Equal("1", SockGrouping.NextGroup(["0", "0", "1", "2"]));
        }

        /// <summary>With all pairs complete, the smallest unused group is chosen, filling gaps.</summary>
        [Fact]
        public void FillsSmallestUnusedGroupWhenAllPaired()
        {
            Assert.Equal("1", SockGrouping.NextGroup(["0", "0", "2", "2"]));
        }

        /// <summary>Negative, null, and non-integer groups are ignored.</summary>
        [Fact]
        public void IgnoresNegativeNullAndNonInteger()
        {
            Assert.Equal("0", SockGrouping.NextGroup([null, "x", "-3"]));
        }
    }
}

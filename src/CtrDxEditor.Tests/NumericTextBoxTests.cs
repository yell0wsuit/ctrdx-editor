using CtrDxEditor.Controls;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the whole-number acceptance rule behind the numeric property box.</summary>
    public class NumericTextBoxTests
    {
        /// <summary>Empty, a lone minus, and in-range integers (including negatives) are accepted.</summary>
        [Theory]
        [InlineData("")]
        [InlineData("-")]
        [InlineData("0")]
        [InlineData("100")]
        [InlineData("9999")]
        [InlineData("-9999")]
        public void AcceptsNumbersAndPrefixes(string text)
        {
            Assert.True(NumericTextBox.IsAcceptable(text, -9999, 9999));
        }

        /// <summary>Letters, symbols, decimals, and out-of-range values are rejected.</summary>
        [Theory]
        [InlineData("a")]
        [InlineData("1a")]
        [InlineData("1.5")]
        [InlineData("1 000")]
        [InlineData("10000")]
        [InlineData("-10000")]
        [InlineData("--1")]
        public void RejectsNonNumbersAndOutOfRange(string text)
        {
            Assert.False(NumericTextBox.IsAcceptable(text, -9999, 9999));
        }

        /// <summary>A non-negative range refuses the minus sign entirely.</summary>
        [Fact]
        public void NonNegativeRangeRejectsMinus()
        {
            Assert.False(NumericTextBox.IsAcceptable("-", 0, 9999));
            Assert.False(NumericTextBox.IsAcceptable("-5", 0, 9999));
        }
    }
}

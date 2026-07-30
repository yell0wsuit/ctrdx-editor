using CtrDxEditor.Playtest;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for parsing the game's stdout playtest handshake.</summary>
    public class PlaytestHandshakeLineTests
    {
        /// <summary>Verifies a well-formed handshake yields its protocol and version.</summary>
        [Fact]
        public void WellFormedLineReturnsProtocolAndVersion()
        {
            bool ok = PlaytestHandshakeLine.TryParse("ctrdx-playtest 1 1.0.0", out int protocol, out string version);

            Assert.True(ok);
            Assert.Equal(1, protocol);
            Assert.Equal("1.0.0", version);
        }

        /// <summary>Verifies a version carrying build metadata survives parsing intact.</summary>
        [Fact]
        public void InformationalVersionWithMetadataIsKeptWhole()
        {
            bool ok = PlaytestHandshakeLine.TryParse("ctrdx-playtest 1 1.0.0+abc1234", out _, out string version);

            Assert.True(ok);
            Assert.Equal("1.0.0+abc1234", version);
        }

        /// <summary>Verifies surrounding whitespace on the line is ignored.</summary>
        [Fact]
        public void SurroundingWhitespaceIsTrimmed()
        {
            bool ok = PlaytestHandshakeLine.TryParse("  ctrdx-playtest 2 1.2.3  ", out int protocol, out string version);

            Assert.True(ok);
            Assert.Equal(2, protocol);
            Assert.Equal("1.2.3", version);
        }

        /// <summary>Verifies malformed lines and unrelated process output are rejected.</summary>
        /// <param name="line">A line that is not a valid handshake.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("some unrelated game log line")]
        [InlineData("ctrdx-playtest")]              // signature only
        [InlineData("ctrdx-playtest 1")]            // no version
        [InlineData("ctrdx-playtest x 1.0.0")]      // non-integer protocol
        [InlineData("CTRDX-PLAYTEST 1 1.0.0")]      // signature is case-sensitive
        [InlineData("notctrdx-playtest 1 1.0.0")]   // near-miss signature
        public void MalformedOrForeignLinesReturnFalse(string? line)
        {
            bool ok = PlaytestHandshakeLine.TryParse(line, out int protocol, out string version);

            Assert.False(ok);
            Assert.Equal(0, protocol);
            Assert.Equal("", version);
        }
    }
}

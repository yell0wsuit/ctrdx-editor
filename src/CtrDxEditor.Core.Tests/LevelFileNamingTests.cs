using System.IO;
using System.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the level-name to file-name conversion used for save-dialog suggestions.</summary>
    public class LevelFileNamingTests
    {
        /// <summary>An ordinary name is suggested as typed, spaces and casing included.</summary>
        [Fact]
        public void KeepsAnOrdinaryName()
        {
            Assert.Equal("Bath Time", LevelFileNaming.Sanitize("Bath Time"));
            Assert.Equal("Bath Time.xml", LevelFileNaming.Suggest("Bath Time", "xml"));
        }

        /// <summary>A level with no name falls back rather than suggesting an extension on its own.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FallsBackWhenUnnamed(string? levelName)
        {
            Assert.Equal(LevelFileNaming.Fallback, LevelFileNaming.Sanitize(levelName));
            Assert.Equal("level.png", LevelFileNaming.Suggest(levelName, "png"));
        }

        /// <summary>
        /// Every character Windows forbids becomes an underscore, even on platforms whose own rules allow
        /// it, so a level named on macOS still saves on Windows.
        /// </summary>
        [Theory]
        [InlineData("a<b", "a_b")]
        [InlineData("a>b", "a_b")]
        [InlineData("a:b", "a_b")]
        [InlineData("a\"b", "a_b")]
        [InlineData("a/b", "a_b")]
        [InlineData("a\\b", "a_b")]
        [InlineData("a|b", "a_b")]
        [InlineData("a?b", "a_b")]
        [InlineData("a*b", "a_b")]
        [InlineData("Box 5: <Spider> \"Season\"?", "Box 5_ _Spider_ _Season__")]
        public void ReplacesForbiddenCharacters(string levelName, string expected)
        {
            Assert.Equal(expected, LevelFileNaming.Sanitize(levelName));
        }

        /// <summary>Control characters are replaced too, so a pasted newline cannot reach the file name.</summary>
        [Fact]
        public void ReplacesControlCharacters()
        {
            Assert.Equal("a_b_c", LevelFileNaming.Sanitize("a\nb\tc"));
        }

        /// <summary>The result never contains a character the running platform rejects.</summary>
        [Fact]
        public void ResultIsValidOnThisPlatform()
        {
            string sanitized = LevelFileNaming.Sanitize("i:n/v*a?l\\i\"d<n>a|m\te");

            Assert.DoesNotContain(sanitized, character => Path.GetInvalidFileNameChars().Contains(character));
        }

        /// <summary>
        /// Surrounding whitespace and trailing dots are dropped, because Windows strips them when creating
        /// the file and the name shown would not be the name written.
        /// </summary>
        [Theory]
        [InlineData("  Spiders  ", "Spiders")]
        [InlineData("Spiders...", "Spiders")]
        [InlineData("Spiders. . .", "Spiders")]
        [InlineData("...", "level")]
        public void TrimsWhitespaceAndTrailingDots(string levelName, string expected)
        {
            Assert.Equal(expected, LevelFileNaming.Sanitize(levelName));
        }

        /// <summary>A long name is capped, and capping never leaves a trailing dot or space behind.</summary>
        [Fact]
        public void CapsLongNames()
        {
            string sanitized = LevelFileNaming.Sanitize(new string('a', 200));
            Assert.Equal(LevelFileNaming.MaxLength, sanitized.Length);

            string trimmedAtCap = LevelFileNaming.Sanitize(new string('a', LevelFileNaming.MaxLength - 1) + ". tail");
            Assert.Equal(new string('a', LevelFileNaming.MaxLength - 1), trimmedAtCap);
        }

        /// <summary>Windows device names get a suffix, since it refuses them whatever the extension.</summary>
        [Theory]
        [InlineData("CON", "CON_")]
        [InlineData("nul", "nul_")]
        [InlineData("Com1", "Com1_")]
        [InlineData("LPT9", "LPT9_")]
        [InlineData("CONSOLE", "CONSOLE")]
        public void SuffixesReservedDeviceNames(string levelName, string expected)
        {
            Assert.Equal(expected, LevelFileNaming.Sanitize(levelName));
        }

        /// <summary>A name made entirely of forbidden characters still yields something writable.</summary>
        [Fact]
        public void NameOfOnlyForbiddenCharactersSurvivesAsUnderscores()
        {
            Assert.Equal("___", LevelFileNaming.Sanitize("///"));
        }
    }
}

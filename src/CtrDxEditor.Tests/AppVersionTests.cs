using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the title-bar version formatting in <see cref="AppVersion"/>.</summary>
    public class AppVersionTests
    {
        /// <summary>Verifies that a dev build's full commit hash is trimmed to seven characters.</summary>
        [Fact]
        public void ShortensTheCommitHashInDevBuilds()
        {
            Assert.Equal(
                "1.0.0-dirty+3dff1f1",
                AppVersion.Shorten("1.0.0-dirty+3dff1f181b3ea69e09eb7a2d28a37c160c85fd3b"));
        }

        /// <summary>Verifies that a release version, which carries no commit hash, is left as is.</summary>
        [Fact]
        public void LeavesReleaseVersionsUntouched()
        {
            Assert.Equal("1.2.3", AppVersion.Shorten("1.2.3"));
        }

        /// <summary>Verifies that an already-short commit hash is not trimmed further.</summary>
        [Fact]
        public void LeavesAlreadyShortCommitsUntouched()
        {
            Assert.Equal("1.0.0+3dff1f1", AppVersion.Shorten("1.0.0+3dff1f1"));
        }

        /// <summary>Verifies the fallback used when no version attribute was stamped into the assembly.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FallsBackWhenNoVersionIsStamped(string? informational)
        {
            Assert.Equal("unknown", AppVersion.Shorten(informational));
        }

        /// <summary>Verifies that the build actually stamps a version the title bar can show.</summary>
        [Fact]
        public void ExposesAVersionForTheRunningAssembly()
        {
            Assert.NotEqual("unknown", AppVersion.Display);
        }
    }
}

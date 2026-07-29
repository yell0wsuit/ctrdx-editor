using System;

using CtrDxEditor.Update;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the release-tag comparison behind the startup update check.</summary>
    public class UpdateVersionTests
    {
        /// <summary>Verifies that a published release build is allowed to check for updates.</summary>
        [Fact]
        public void TreatsABareVersionAsAReleaseBuild()
        {
            Assert.True(UpdateVersion.IsReleaseBuild("1.0.0"));
        }

        /// <summary>
        /// Verifies that builds from source never check, so working at the released version number
        /// does not prompt on every launch.
        /// </summary>
        [Theory]
        [InlineData("1.0.0-dirty+3dff1f1")]
        [InlineData("1.0.0-dirty")]
        [InlineData("1.0.0+3dff1f1")]
        [InlineData("1.1.0-beta.1")]
        public void RejectsBuildsCarryingAPrereleaseOrCommitMarker(string informational)
        {
            Assert.False(UpdateVersion.IsReleaseBuild(informational));
        }

        /// <summary>Verifies that an unstamped or unparsable version never reaches the network.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown")]
        [InlineData("1.0")]
        public void RejectsVersionsItCannotCompare(string? informational)
        {
            Assert.False(UpdateVersion.IsReleaseBuild(informational));
        }

        /// <summary>Verifies that the leading "v" the release workflow tags with is stripped.</summary>
        [Fact]
        public void ParsesTagsWithAndWithoutTheLeadingV()
        {
            Assert.Equal(new Version(1, 2, 3), UpdateVersion.Parse("v1.2.3"));
            Assert.Equal(new Version(1, 2, 3), UpdateVersion.Parse("1.2.3"));
        }

        /// <summary>
        /// Verifies that a four-part version drops its revision, so the assembly-name fallback
        /// (<c>1.0.0.0</c>) does not compare as newer than the three-part tag <c>1.0.0</c>.
        /// </summary>
        [Fact]
        public void NormalizesAFourthComponentAway()
        {
            Assert.Equal(new Version(1, 0, 0), UpdateVersion.Parse("1.0.0.0"));
            Assert.False(UpdateVersion.IsNewer("1.0.0.0", "v1.0.0"));
            Assert.False(UpdateVersion.IsNewer("1.0.0", "v1.0.0.0"));
        }

        /// <summary>Verifies that malformed tags are rejected rather than partially parsed.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("latest")]
        [InlineData("v1.2")]
        [InlineData("1.2.3-beta")]
        [InlineData("1..3")]
        [InlineData("-1.2.3")]
        public void RejectsMalformedTags(string? tag)
        {
            Assert.Null(UpdateVersion.Parse(tag));
        }

        /// <summary>Verifies that a strictly newer published tag reports an update.</summary>
        [Theory]
        [InlineData("1.0.0", "v1.0.1")]
        [InlineData("1.0.0", "v1.1.0")]
        [InlineData("1.0.9", "v1.1.0")]
        [InlineData("1.9.9", "v2.0.0")]
        public void ReportsAnUpdateForANewerTag(string local, string tag)
        {
            Assert.True(UpdateVersion.IsNewer(local, tag));
        }

        /// <summary>Verifies that the running version and older tags do not prompt.</summary>
        [Theory]
        [InlineData("1.0.0", "v1.0.0")]
        [InlineData("1.0.1", "v1.0.0")]
        [InlineData("1.1.0", "v1.0.9")]
        [InlineData("2.0.0", "v1.9.9")]
        public void ReportsNoUpdateForTheSameOrOlderTag(string local, string tag)
        {
            Assert.False(UpdateVersion.IsNewer(local, tag));
        }

        /// <summary>Verifies that a dev build stays silent even when a newer release exists.</summary>
        [Fact]
        public void NeverPromptsFromADevBuild()
        {
            Assert.False(UpdateVersion.IsNewer("1.0.0-dirty+3dff1f1", "v9.0.0"));
        }

        /// <summary>Verifies that an unusable tag is treated as "no update" rather than as an error.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("nightly")]
        public void ReportsNoUpdateForAnUnusableTag(string? tag)
        {
            Assert.False(UpdateVersion.IsNewer("1.0.0", tag));
        }
    }
}

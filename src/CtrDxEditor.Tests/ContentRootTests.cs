using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that content and settings paths hang off the resolved user data directory.</summary>
    public class ContentRootTests
    {
        /// <summary>Verifies the settings file lives under the resolved data directory.</summary>
        [Fact]
        public void SettingsPathIsUnderUserDataDirectory()
        {
            Assert.Equal(
                Path.Combine(UserDataDirectory.Current, "settings.json"),
                ContentRoot.SettingsPath);
        }

        /// <summary>Verifies the download destination lives under the resolved data directory.</summary>
        [Fact]
        public void DefaultContentDirIsUnderUserDataDirectory()
        {
            Assert.Equal(
                Path.Combine(UserDataDirectory.Current, "content"),
                ContentRoot.DefaultContentDir);
        }
    }
}

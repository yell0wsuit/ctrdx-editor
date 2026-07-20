using System;
using System.Collections.Generic;
using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the writable-directory fallback chain in <see cref="UserDataDirectory"/>.</summary>
    public class UserDataDirectoryTests
    {
        private const string Documents = "/home/u/Documents";
        private const string LocalAppData = "/home/u/.local/share";

        // Treats every candidate as writable except those listed.
        private static Func<string, bool> WritableExcept(params string[] unwritable)
        {
            HashSet<string> denied = new(unwritable, StringComparer.Ordinal);
            return path => !denied.Contains(path);
        }

        /// <summary>Verifies the executable directory wins when it is writable (portable use).</summary>
        [Fact]
        public void PrefersExecutableDirectoryWhenWritable()
        {
            string resolved = UserDataDirectory.Resolve(
                "/opt/editor", Documents, LocalAppData, WritableExcept());

            Assert.Equal("/opt/editor", resolved);
        }

        /// <summary>Verifies a read-only executable directory (AppImage) falls through to Documents.</summary>
        [Fact]
        public void FallsBackToDocumentsWhenExecutableDirectoryIsReadOnly()
        {
            string resolved = UserDataDirectory.Resolve(
                "/mnt/appimage", Documents, LocalAppData, WritableExcept("/mnt/appimage"));

            Assert.Equal(Path.Combine(Documents, "CtrDxEditorData"), resolved);
        }

        /// <summary>Verifies the executable directory is skipped entirely inside a macOS .app bundle.</summary>
        [Fact]
        public void SkipsExecutableDirectoryInsideMacAppBundle()
        {
            string resolved = UserDataDirectory.Resolve(
                "/Applications/CtrDxEditor.app/Contents/MacOS",
                Documents,
                LocalAppData,
                WritableExcept());

            Assert.Equal(Path.Combine(Documents, "CtrDxEditorData"), resolved);
        }

        /// <summary>Verifies the third candidate is used when Documents is unwritable.</summary>
        [Fact]
        public void FallsBackToLocalAppDataWhenDocumentsUnwritable()
        {
            string resolved = UserDataDirectory.Resolve(
                "/mnt/appimage",
                Documents,
                LocalAppData,
                WritableExcept("/mnt/appimage", Path.Combine(Documents, "CtrDxEditorData")));

            Assert.Equal(Path.Combine(LocalAppData, "CtrDxEditorData"), resolved);
        }

        /// <summary>Verifies the last-resort current directory when every candidate fails.</summary>
        [Fact]
        public void ReturnsCurrentDirectoryWhenAllCandidatesFail()
        {
            string resolved = UserDataDirectory.Resolve(
                "/mnt/appimage", Documents, LocalAppData, _ => false);

            Assert.Equal(".", resolved);
        }

        /// <summary>Verifies bundle detection matches the .app/Contents/MacOS structure at any depth.</summary>
        [Theory]
        [InlineData("/Applications/CtrDxEditor.app/Contents/MacOS", true)]
        [InlineData("/Applications/CtrDxEditor.app/Contents/MacOS/sub/dir", true)]
        [InlineData("/Users/u/build/publish/osx-arm64", false)]
        [InlineData("/Applications/CtrDxEditor.app/Contents/Resources", false)]
        [InlineData("/opt/editor", false)]
        public void DetectsMacAppBundlePaths(string path, bool expected)
        {
            Assert.Equal(expected, UserDataDirectory.IsInsideMacAppBundle(path));
        }

        /// <summary>Verifies the writability probe succeeds for a real temp directory.</summary>
        [Fact]
        public void IsWritableTrueForTempDirectory()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-udd-").FullName;
            try
            {
                Assert.True(UserDataDirectory.IsWritable(Path.Combine(root, "created-on-demand")));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies the resolved directory is cached and non-empty in the test environment.</summary>
        [Fact]
        public void CurrentReturnsAStableNonEmptyPath()
        {
            Assert.False(string.IsNullOrEmpty(UserDataDirectory.Current));
            Assert.Equal(UserDataDirectory.Current, UserDataDirectory.Current);
        }
    }
}

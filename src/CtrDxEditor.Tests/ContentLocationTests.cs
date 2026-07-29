using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for resolving and validating editor content directories.</summary>
    public class ContentLocationTests
    {
        // Creates a valid content dir (manifest + the one file it lists) at parent/name.
        private static string MakeValidContent(string parent, string name)
        {
            string dir = Path.Combine(parent, name);
            _ = Directory.CreateDirectory(Path.Combine(dir, "images"));
            File.WriteAllText(Path.Combine(dir, "images", "a.png"), "x");
            File.WriteAllText(
                Path.Combine(dir, ContentManifest.FileName),
                                     /*lang=json,strict*/
                                     """{"files":{"images/a.png":"_"}}""");
            return dir;
        }

        /// <summary>Verifies that a manifest-complete content directory is valid.</summary>
        [Fact]
        public void IsValidTrueForCompleteContentDir()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string content = MakeValidContent(root, "content");
                Assert.True(ContentLocation.IsValid(content));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that empty and null content directories are invalid.</summary>
        [Fact]
        public void IsValidFalseForEmptyDirAndNull()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                Assert.False(ContentLocation.IsValid(root));
                Assert.False(ContentLocation.IsValid(null));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that a valid configured content path takes priority.</summary>
        [Fact]
        public void ResolvePrefersValidConfiguredPath()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string configured = MakeValidContent(root, "configured");
                _ = MakeValidContent(root, "content"); // next-to-exe candidate also valid

                string? resolved = ContentLocation.Resolve(root, configured);

                Assert.Equal(configured, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies fallback to a content directory next to the base directory.</summary>
        [Fact]
        public void ResolveFallsBackToContentNextToBaseDir()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string local = MakeValidContent(root, "content");

                string? resolved = ContentLocation.Resolve(root, configuredPath: null);

                Assert.Equal(local, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies fallback to an ancestor content directory.</summary>
        [Fact]
        public void ResolveWalksUpToAncestorContent()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string ancestorContent = MakeValidContent(root, "content");
                string deepBase = Path.Combine(root, "a", "b", "c");
                _ = Directory.CreateDirectory(deepBase);

                string? resolved = ContentLocation.Resolve(deepBase, configuredPath: null);

                Assert.Equal(ancestorContent, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that resolution returns null when no valid content directory exists.</summary>
        [Fact]
        public void ResolveReturnsNullWhenNothingValid()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                Assert.Null(ContentLocation.Resolve(root, configuredPath: null));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies that search roots are tried in order, first valid winning.</summary>
        [Fact]
        public void ResolveTriesSearchRootsInOrder()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string first = Path.Combine(root, "first");
                string second = Path.Combine(root, "second");
                _ = Directory.CreateDirectory(first);
                string secondContent = MakeValidContent(second, "content");

                string? resolved = ContentLocation.Resolve([first, second], configuredPath: null);

                Assert.Equal(secondContent, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies an earlier search root wins over a later one.</summary>
        [Fact]
        public void ResolvePrefersEarlierSearchRoot()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string first = Path.Combine(root, "first");
                string second = Path.Combine(root, "second");
                string firstContent = MakeValidContent(first, "content");
                _ = MakeValidContent(second, "content");

                string? resolved = ContentLocation.Resolve([first, second], configuredPath: null);

                Assert.Equal(firstContent, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies a valid configured path still outranks every search root.</summary>
        [Fact]
        public void ResolvePrefersConfiguredPathOverSearchRoots()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                string configured = MakeValidContent(root, "configured");
                string other = Path.Combine(root, "other");
                _ = MakeValidContent(other, "content");

                string? resolved = ContentLocation.Resolve([other], configured);

                Assert.Equal(configured, resolved);
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>Verifies null is returned when no search root holds valid content.</summary>
        [Fact]
        public void ResolveReturnsNullWhenNoSearchRootValid()
        {
            string root = Directory.CreateTempSubdirectory("ctrdx-loc-").FullName;
            try
            {
                Assert.Null(ContentLocation.Resolve([root], configuredPath: null));
            }
            finally { Directory.Delete(root, recursive: true); }
        }

        /// <summary>
        /// A configured path pointing somewhere other than the install directory must be repointed;
        /// resolution prefers it, so leaving it would keep loading the superseded content and
        /// re-prompt on every launch.
        /// </summary>
        [Fact]
        public void ShouldRepointAConfiguredPathAwayFromTheInstallDirectory()
        {
            Assert.True(ContentLocation.ShouldRepoint(
                Path.Combine("/somewhere", "else", "content"),
                Path.Combine("/data", "content")));
        }

        /// <summary>An unset path already falls through to the install directory, so it is left alone.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DoesNotRepointAnUnsetPath(string? configured)
        {
            Assert.False(ContentLocation.ShouldRepoint(configured, Path.Combine("/data", "content")));
        }

        /// <summary>A path already naming the install directory needs no write.</summary>
        [Fact]
        public void DoesNotRepointAPathAlreadyAtTheInstallDirectory()
        {
            string installed = Path.Combine(Path.GetTempPath(), "ctrdx-content");

            Assert.False(ContentLocation.ShouldRepoint(installed, installed));
        }

        /// <summary>
        /// The same directory written differently is still the same directory, so a trailing separator
        /// or a relative segment must not trigger a pointless rewrite.
        /// </summary>
        [Fact]
        public void DoesNotRepointTheSameDirectoryWrittenDifferently()
        {
            string installed = Path.Combine(Path.GetTempPath(), "ctrdx-content");

            Assert.False(ContentLocation.ShouldRepoint(installed + Path.DirectorySeparatorChar, installed));
            Assert.False(ContentLocation.ShouldRepoint(
                Path.Combine(installed, "..", "ctrdx-content"), installed));
        }
    }
}

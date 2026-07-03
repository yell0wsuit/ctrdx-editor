using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
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
    }
}

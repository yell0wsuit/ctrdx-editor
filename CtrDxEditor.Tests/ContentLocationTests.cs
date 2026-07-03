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
        public void IsValid_true_for_complete_content_dir()
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
        public void IsValid_false_for_empty_dir_and_null()
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
        public void Resolve_prefers_valid_configured_path()
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
        public void Resolve_falls_back_to_content_next_to_base_dir()
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
        public void Resolve_walks_up_to_ancestor_content()
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
        public void Resolve_returns_null_when_nothing_valid()
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

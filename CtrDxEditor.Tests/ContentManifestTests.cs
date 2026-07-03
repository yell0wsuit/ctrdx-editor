using System.Collections.Generic;
using System.IO;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    public class ContentManifestTests
    {
        [Fact]
        public void Read_parses_files_section()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-manifest-").FullName;
            try
            {
                string path = Path.Combine(dir, ContentManifest.FileName);
                File.WriteAllText(path, /*lang=json,strict*/ """{"files":{"images/a.png":"abc","fonts/b.ttf":"def"}}""");

                IReadOnlyDictionary<string, string> manifest = ContentManifest.Read(path);

                Assert.Equal(2, manifest.Count);
                Assert.Equal("abc", manifest["images/a.png"]);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void MissingFiles_lists_only_absent_entries()
        {
            string dir = Directory.CreateTempSubdirectory("ctrdx-manifest-").FullName;
            try
            {
                _ = Directory.CreateDirectory(Path.Combine(dir, "images"));
                File.WriteAllText(Path.Combine(dir, "images", "present.png"), "x");
                Dictionary<string, string> manifest = new()
                {
                    ["images/present.png"] = "_",
                    ["images/absent.png"] = "_",
                };

                IReadOnlyList<string> missing = ContentManifest.MissingFiles(dir, manifest);

                _ = Assert.Single(missing);
                Assert.Equal("images/absent.png", missing[0]);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }
    }
}

using System;
using System.IO;

namespace CtrDxEditor.Content
{
    /// <summary>Locates the repository's content/ directory by walking up from the app base dir.</summary>
    public static class ContentRoot
    {
        public static string Resolve()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "content");
                if (File.Exists(Path.Combine(candidate, "file_manifest.json")))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate the 'content' directory in any parent of " + AppContext.BaseDirectory +
                ". Expected a 'content' folder containing 'file_manifest.json'.");
        }
    }
}

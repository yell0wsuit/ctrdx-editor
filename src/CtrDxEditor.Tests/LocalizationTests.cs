using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests localized UI strings that are easy to miss during dialog wiring.</summary>
    public class LocalizationTests
    {
        /// <summary>Verifies the close confirmation dialog has a visible action-confirmation header.</summary>
        [Fact]
        public void CloseConfirmationHeaderIsLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Confirm your action", strings["Dialog.Close.Header"]);
        }

        private static string FindRepositoryFile(string relativePath)
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }

            throw new FileNotFoundException("Could not find repository file.", relativePath);
        }
    }
}

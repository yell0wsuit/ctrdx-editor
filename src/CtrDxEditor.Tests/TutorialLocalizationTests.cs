using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tutorial object, attribute, and icon labels exist in the English localization file.</summary>
    public class TutorialLocalizationTests
    {
        private static Dictionary<string, string> Strings()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
        }

        private static string FindRepositoryFile(string relativePath)
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find repository file.", relativePath);
        }

        /// <summary>Localizes the tutorial icon and tutorial text object names.</summary>
        [Fact]
        public void ObjectNamesAreLocalized()
        {
            Dictionary<string, string> strings = Strings();
            Assert.Equal("Tutorial icon", strings["Object.tutorial"]);
            Assert.Equal("Tutorial text", strings["Object.tutorialText"]);
        }

        /// <summary>Localizes the icon field, literal text field, and representative icon options.</summary>
        [Fact]
        public void IconOptionsAreLocalized()
        {
            Dictionary<string, string> strings = Strings();
            Assert.Equal("Cut line", strings["Attr.icon.tutorial01"]);
            Assert.Equal("Cursor", strings["Attr.icon.tutorial10"]);
            Assert.Equal("Fingers", strings["Attr.icon.tutorial11"]);
            Assert.True(strings.ContainsKey("Attr.icon"));
            Assert.True(strings.ContainsKey("Attr.text"));
        }
    }
}

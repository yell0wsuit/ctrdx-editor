using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Verifies the conveyor's localized labels exist. Asserts against en.json directly, matching the
    /// suite's other localization tests: the xUnit host has no Avalonia AssetLoader, so a runtime
    /// <c>Localizer</c> lookup would fall back to the raw key rather than read the embedded resource.
    /// </summary>
    public class ConveyorLocalizationTests
    {
        private static Dictionary<string, string> Strings()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
        }

        [Fact]
        public void ConveyorObjectNameIsLocalized()
        {
            Assert.Equal("Conveyor", Strings()["Object.transporter"]);
        }

        [Fact]
        public void ConveyorAttributeLabelsAreLocalized()
        {
            Dictionary<string, string> strings = Strings();
            Assert.Equal("Velocity", strings["Attr.velocity"]);
            Assert.Equal("Width", strings["Attr.width"]);
            Assert.Equal("Direction", strings["Attr.direction"]);
            Assert.Equal("Automatic", strings["Attr.auto"]);
        }

        [Fact]
        public void DirectionOptionsAreLocalized()
        {
            Dictionary<string, string> strings = Strings();
            Assert.Equal("Forward", strings["Attr.direction.forward"]);
            Assert.Equal("Backward", strings["Attr.direction.backward"]);
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

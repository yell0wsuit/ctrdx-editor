using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using CtrDxEditor.Localization;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests localized UI strings that are easy to miss during dialog wiring.</summary>
    public class LocalizationTests
    {
        /// <summary>Verifies the unsaved-changes confirmation dialog has a visible header.</summary>
        [Fact]
        public void UnsavedChangesHeaderIsLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Unsaved changes", strings["Dialog.Unsaved.Header"]);
            Assert.DoesNotContain("Dialog.Close.Header", strings.Keys);
        }

        /// <summary>Verifies the magic hat and its pairing field have user-facing English text.</summary>
        [Fact]
        public void MagicHatObjectAndGroupAreLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Magic hat", strings["Object.sock"]);
            Assert.Equal("Teleport group", strings["Attr.sockGroup"]);
            Assert.DoesNotContain("Attr.group", strings.Keys);
        }

        /// <summary>Both XML width variants resolve through one family-level translation key.</summary>
        [Fact]
        public void BouncerVariantsAreLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Bouncer", strings["Object.bouncer"]);
            Assert.DoesNotContain("Object.bouncer1", strings.Keys);
            Assert.DoesNotContain("Object.bouncer2", strings.Keys);
            Assert.Equal("Bouncer", Localizer.ObjectName("bouncer1"));
            Assert.Equal("Bouncer", Localizer.ObjectName("bouncer2"));
        }

        /// <summary>The mouse and its active-time and index fields have user-facing English text.</summary>
        [Fact]
        public void MouseObjectAndFieldsAreLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Mouse", strings["Object.gap"]);
            Assert.Equal("Mouse", Localizer.ObjectName("gap"));
            Assert.Equal("Active time (s)", strings["Attr.activeTime"]);
            Assert.Equal("Index", strings["Attr.index"]);
            Assert.DoesNotContain("Object.mouse", strings.Keys);
        }

        /// <summary>The moving-grab pollen toggle has user-facing text.</summary>
        [Fact]
        public void HidePathIsLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Hide pollen path", strings["Attr.hidePath"]);
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

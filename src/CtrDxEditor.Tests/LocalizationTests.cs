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
        /// <summary>Verifies the close confirmation dialog has a visible action-confirmation header.</summary>
        [Fact]
        public void CloseConfirmationHeaderIsLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Confirm your action", strings["Dialog.Close.Header"]);
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

        /// <summary>The ghost object and its morph toggles are localized; radius/angle reuse existing keys.</summary>
        [Fact]
        public void GhostObjectAndMorphTogglesAreLocalized()
        {
            string path = FindRepositoryFile("src/CtrDxEditor.Shared/Localization/en.json");
            Dictionary<string, string> strings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(path))!;

            Assert.Equal("Ghost", strings["Object.ghost"]);
            Assert.Equal("Ghost", Localizer.ObjectName("ghost"));
            Assert.Equal("Grab", strings["Attr.grab"]);
            Assert.Equal("Bubble", strings["Attr.bubble"]);
            Assert.Equal("Bouncer", strings["Attr.bouncer"]);

            // radius and angle reuse the shared grab/bouncer attribute strings rather than ghost-specific keys.
            Assert.Equal("Radius", strings["Attr.radius"]);
            Assert.Equal("Angle", strings["Attr.angle"]);
            Assert.DoesNotContain("Attr.ghostRadius", strings.Keys);
            Assert.DoesNotContain("Attr.ghostAngle", strings.Keys);
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

using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards the app-owned Inter registration used by desktop, browser, and rendering code.</summary>
    public class BundledInterFontTests
    {
        /// <summary>The app no longer downloads the duplicate Avalonia Inter package.</summary>
        [Fact]
        public void ProjectDoesNotReferenceAvaloniaInterPackage()
        {
            string versions = File.ReadAllText(RepositoryPath("Directory.Packages.props"));
            string sharedProject = File.ReadAllText(
                RepositoryPath("src", "CtrDxEditor.Shared", "CtrDxEditor.Shared.csproj"));

            Assert.DoesNotContain("Avalonia.Fonts.Inter", versions, StringComparison.Ordinal);
            Assert.DoesNotContain("Avalonia.Fonts.Inter", sharedProject, StringComparison.Ordinal);
        }

        /// <summary>Both application hosts register the app-owned font collection.</summary>
        [Fact]
        public void HostsRegisterBundledInterCollection()
        {
            string desktop = File.ReadAllText(
                RepositoryPath("src", "CtrDxEditor.Desktop", "Program.cs"));
            string browser = File.ReadAllText(
                RepositoryPath("src", "CtrDxEditor.Browser", "Program.cs"));

            Assert.Contains(".WithBundledInterFont()", desktop, StringComparison.Ordinal);
            Assert.Contains(".WithBundledInterFont()", browser, StringComparison.Ordinal);
            Assert.DoesNotContain(".WithInterFont()", desktop, StringComparison.Ordinal);
            Assert.DoesNotContain(".WithInterFont()", browser, StringComparison.Ordinal);
        }

        /// <summary>UI registration and Skia fallback resolve the same shared Inter assets.</summary>
        [Fact]
        public void RegistrationAndTutorialFallbackUseSharedFontResources()
        {
            string registration = File.ReadAllText(
                RepositoryPath("src", "CtrDxEditor.Shared", "Startup", "BundledInterFont.cs"));
            string tutorial = File.ReadAllText(
                RepositoryPath(
                    "src",
                    "CtrDxEditor.Shared",
                    "Rendering",
                    "TutorialTextDrawOperation.cs"));

            Assert.Contains("fonts:Inter", registration, StringComparison.Ordinal);
            Assert.Contains(
                "avares://CtrDxEditor.Shared/Assets/Fonts/Inter",
                registration,
                StringComparison.Ordinal);
            Assert.Contains(
                "avares://CtrDxEditor.Shared/Assets/Fonts/Inter/Inter-Regular.ttf",
                tutorial,
                StringComparison.Ordinal);
            Assert.DoesNotContain("avares://Avalonia.Fonts.Inter", tutorial, StringComparison.Ordinal);
        }

        private static string RepositoryPath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([Directory.GetParent(path)!.FullName, .. parts]);
        }
    }
}

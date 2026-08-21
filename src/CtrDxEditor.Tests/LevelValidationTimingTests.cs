using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards which user actions trigger level validation.</summary>
    public sealed class LevelValidationTimingTests
    {
        /// <summary>Opening only parses; validation remains at the save and playtest boundaries.</summary>
        [Fact]
        public void ValidationRunsForSaveAndPlaytestButNotOpen()
        {
            string fileCommands = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Views", "MainView.FileCommands.cs"));
            string playtestCommands = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));

            string open = SliceBetween(fileCommands, "private async Task OpenLevelFileAsync", "private async void Close_Click");
            string save = SliceBetween(fileCommands, "private static async Task<bool> CanSaveAsync", "private static async Task WriteXmlAsync");

            Assert.DoesNotContain("LevelValidator.Validate(", open, StringComparison.Ordinal);
            Assert.Contains("LevelValidator.Validate(doc)", save, StringComparison.Ordinal);
            Assert.Contains("LevelValidator.Validate(doc)", playtestCommands, StringComparison.Ordinal);
        }

        private static string SliceBetween(string source, string startMarker, string endMarker)
        {
            int start = source.IndexOf(startMarker, StringComparison.Ordinal);
            int end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            return source[start..end];
        }

        private static string SourcePath(params string[] parts)
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null && !Directory.Exists(Path.Combine(dir, "CtrDxEditor.Shared")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }
            Assert.NotNull(dir);
            return Path.Combine([dir, .. parts]);
        }
    }
}

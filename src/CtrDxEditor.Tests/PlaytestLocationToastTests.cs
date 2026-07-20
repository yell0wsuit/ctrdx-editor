using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// The toast confirming a newly set Cut the Rope: DX location. Asserted against the source because
    /// the command is a click handler with no headless harness to raise it in.
    /// </summary>
    public class PlaytestLocationToastTests
    {
        /// <summary>
        /// Only the explicit command confirms. The first-run pick happens inside Play, which already
        /// reports itself by launching, so hoisting this toast into the shared helper would stack two
        /// toasts on one action.
        /// </summary>
        [Fact]
        public void OnlyTheExplicitCommandToasts()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));

            int command = source.IndexOf("void SetDxLocation_Click(", StringComparison.Ordinal);
            Assert.True(command >= 0, "SetDxLocation_Click should exist.");

            int toast = source.IndexOf("Notification.Playtest.LocationSet", StringComparison.Ordinal);
            Assert.True(toast > command, "The location toast belongs in SetDxLocation_Click.");

            int helper = source.IndexOf("Task<string?> PickDxExecutableAsync(", StringComparison.Ordinal);
            Assert.True(helper > toast, "The toast should sit outside the helper Play also calls.");
        }

        /// <summary>A cancelled pick returns null and must not be announced as a location change.</summary>
        [Fact]
        public void CancelledPickDoesNotToast()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));

            Assert.Contains(
                "await PickDxExecutableAsync(vm) is { } path",
                source,
                StringComparison.Ordinal);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine(path, Path.Combine(parts));
        }
    }
}

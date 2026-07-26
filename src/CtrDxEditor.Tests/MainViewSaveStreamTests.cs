using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the constraints the browser storage streams put on how the level file is written.</summary>
    public class MainViewSaveStreamTests
    {
        /// <summary>
        /// The browser's writeable stream reports CanSeek, but its Seek/SetLength go through the
        /// File System Access polyfill that Safari and Firefox fall back to - and that polyfill's download
        /// sink pushes the raw {type:"truncate"} command object into a Blob, where it stringifies to
        /// "[object Object]" and lands in the saved file. Saves must therefore write forward only:
        /// OpenWriteAsync already starts from an empty file on every platform (FileMode.Create on desktop,
        /// createWritable({keepExistingData:false}) in the browser).
        /// </summary>
        [Fact]
        public void SavingNeverSeeksOrTruncatesTheDestinationStream()
        {
            string commands = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.FileCommands.cs"));

            Assert.DoesNotContain(".SetLength(", commands, StringComparison.Ordinal);
            Assert.DoesNotContain(".Seek(", commands, StringComparison.Ordinal);
        }

        /// <summary>
        /// Every exit from the screenshot save closes the sticky "Saving…" toast by hand.
        /// </summary>
        /// <remarks>
        /// It is shown with Expiration.Zero, so nothing retires it on its own: an unclosed one sits over
        /// the canvas for the rest of the session. This used to be handled by the notification manager's
        /// MaxItems being 1, which let the terminal toast evict it - an invisible coupling between a flow
        /// here and a number in MainView.axaml.cs that broke the moment toasts were allowed to stack.
        /// </remarks>
        [Fact]
        public void ScreenshotSaveClosesItsStickyToastOnEveryExit()
        {
            string commands = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.FileCommands.cs"));

            Assert.Contains("expiration: TimeSpan.Zero", commands, StringComparison.Ordinal);
            // One Show for the sticky toast, and a Close before each of the two terminal toasts.
            Assert.Equal(1, CountOccurrences(commands, "toasts?.Show(saving);"));
            Assert.Equal(2, CountOccurrences(commands, "toasts?.Close(saving);"));

            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            Assert.DoesNotContain("MaxItems = 1,", view, StringComparison.Ordinal);
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
                 i >= 0;
                 i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}

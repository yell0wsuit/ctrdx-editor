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

using System;
using System.IO;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the file-backed recovery snapshot store.</summary>
    public class FileRecoveryStoreTests
    {
        private static RecoverySnapshot Sample(string xml = "<map><layer name=\"a\" /></map>")
        {
            return new RecoverySnapshot
            {
                Xml = xml,
                BaselineXml = "<map />",
                FileName = "level.xml",
                RopeSkin = 1,
                Background = 2,
                CandySkin = 3,
                OmNomSupport = 4,
                SavedAt = new DateTimeOffset(2026, 9, 15, 14, 32, 0, TimeSpan.Zero),
            };
        }

        private static string TempPath(out string dir)
        {
            dir = Directory.CreateTempSubdirectory("ctrdx-recovery-").FullName;
            return Path.Combine(dir, "nested", "recovery.json");
        }

        /// <summary>A saved snapshot loads back with every field intact.</summary>
        [Fact]
        public async Task SaveThenLoadRoundTrips()
        {
            string path = TempPath(out string dir);
            try
            {
                FileRecoveryStore store = new(path);
                await store.SaveAsync(Sample());

                RecoverySnapshot? loaded = await store.LoadAsync();

                Assert.Equal(Sample(), loaded);
                Assert.False(File.Exists(path + ".tmp"));
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>No file means no snapshot.</summary>
        [Fact]
        public async Task LoadMissingFileReturnsNull()
        {
            string path = TempPath(out string dir);
            try
            {
                Assert.Null(await new FileRecoveryStore(path).LoadAsync());
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Unparseable JSON loads as no snapshot instead of throwing.</summary>
        [Fact]
        public async Task LoadCorruptFileReturnsNull()
        {
            string path = TempPath(out string dir);
            try
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, "{not json");

                Assert.Null(await new FileRecoveryStore(path).LoadAsync());
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>Clearing removes the snapshot, and clearing an empty store is harmless.</summary>
        [Fact]
        public async Task ClearRemovesSnapshot()
        {
            string path = TempPath(out string dir);
            try
            {
                FileRecoveryStore store = new(path);
                await store.ClearAsync();
                await store.SaveAsync(Sample());

                await store.ClearAsync();

                Assert.Null(await store.LoadAsync());
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        /// <summary>A write that fails part way leaves the previous snapshot readable.</summary>
        [Fact]
        public async Task FailedWriteKeepsPreviousSnapshot()
        {
            string path = TempPath(out string dir);
            try
            {
                FileRecoveryStore store = new(path);
                await store.SaveAsync(Sample("<map>first</map>"));
                // A directory squatting on the temp path makes the temp write throw.
                _ = Directory.CreateDirectory(path + ".tmp");

                _ = await Assert.ThrowsAnyAsync<Exception>(() => store.SaveAsync(Sample("<map>second</map>")));

                Assert.Equal("<map>first</map>", (await store.LoadAsync())?.Xml);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }
    }
}

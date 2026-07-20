using System;
using System.IO;

using CtrDxEditor.Playtest;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the playtest session temp store.</summary>
    public class PlaytestTempStoreTests : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("ctrdx-playtest-").FullName;

        /// <inheritdoc />
        public void Dispose()
        {
            Directory.Delete(_root, recursive: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Verifies the first write creates the session directory and the level file.</summary>
        [Fact]
        public void FirstWriteCreatesSessionDirectoryAndFile()
        {
            using PlaytestTempStore store = new(_root);

            string path = store.Write("<map/>");

            Assert.True(File.Exists(path));
            Assert.Equal("<map/>", File.ReadAllText(path));
            Assert.Equal(store.SessionDirectory, Path.GetDirectoryName(path));
        }

        /// <summary>
        /// Verifies every write targets the same path. The game binds a file watcher to the path it
        /// was launched with, so a changing name would silently disable live reload.
        /// </summary>
        [Fact]
        public void EveryWriteUsesTheSameStablePath()
        {
            using PlaytestTempStore store = new(_root);

            string first = store.Write("<map id='1'/>");
            string second = store.Write("<map id='2'/>");
            string third = store.Write("<map id='3'/>");

            Assert.Equal(store.LevelPath, first);
            Assert.Equal(first, second);
            Assert.Equal(second, third);
        }

        /// <summary>Verifies the level path is known before anything has been written.</summary>
        [Fact]
        public void LevelPathIsStableBeforeFirstWrite()
        {
            using PlaytestTempStore store = new(_root);

            string before = store.LevelPath;

            Assert.Equal(before, store.Write("<map/>"));
        }

        /// <summary>Verifies a later write replaces the content rather than appending to it.</summary>
        [Fact]
        public void SecondWriteReplacesContent()
        {
            using PlaytestTempStore store = new(_root);

            _ = store.Write("<map id='old' padding='aaaaaaaaaaaaaaaaaaaa'/>");
            string path = store.Write("<map id='new'/>");

            Assert.Equal("<map id='new'/>", File.ReadAllText(path));
        }

        /// <summary>Verifies repeated writes leave exactly one file, with no scratch file behind.</summary>
        [Fact]
        public void RepeatedWritesLeaveExactlyOneFile()
        {
            using PlaytestTempStore store = new(_root);

            for (int i = 1; i <= 6; i++)
            {
                _ = store.Write($"<map id='{i}'/>");
            }

            _ = Assert.Single(Directory.GetFiles(store.SessionDirectory));
        }

        /// <summary>Verifies disposal removes the whole session directory.</summary>
        [Fact]
        public void DisposeDeletesSessionDirectory()
        {
            PlaytestTempStore store = new(_root);
            _ = store.Write("<map/>");
            string dir = store.SessionDirectory;

            store.Dispose();

            Assert.False(Directory.Exists(dir));
        }

        /// <summary>Verifies disposal is safe when the directory was already removed externally.</summary>
        [Fact]
        public void DisposeOnAlreadyDeletedDirectoryDoesNotThrow()
        {
            PlaytestTempStore store = new(_root);
            _ = store.Write("<map/>");
            Directory.Delete(store.SessionDirectory, recursive: true);

            store.Dispose();
        }

        /// <summary>Verifies disposal is safe when no playtest ever ran (directory never created).</summary>
        [Fact]
        public void DisposeWithoutAnyWriteDoesNotThrow()
        {
            PlaytestTempStore store = new(_root);

            store.Dispose();
        }

        /// <summary>Verifies the sweep deletes a session directory whose owning process is gone.</summary>
        [Fact]
        public void SweepDeletesDirectoryOfDeadProcess()
        {
            string dead = Path.Combine(_root, "playtest-424242-abcdef01");
            _ = Directory.CreateDirectory(dead);
            File.WriteAllText(Path.Combine(dead, "level.xml"), "<map/>");

            PlaytestTempStore.SweepStale(_root, isProcessAlive: _ => false);

            Assert.False(Directory.Exists(dead));
        }

        /// <summary>Verifies the sweep spares a session directory whose owning process is still running.</summary>
        [Fact]
        public void SweepSparesDirectoryOfLiveProcess()
        {
            int alive = Environment.ProcessId;
            string live = Path.Combine(_root, $"playtest-{alive}-abcdef01");
            _ = Directory.CreateDirectory(live);

            PlaytestTempStore.SweepStale(_root, isProcessAlive: pid => pid == alive);

            Assert.True(Directory.Exists(live));
        }

        /// <summary>Verifies the sweep leaves unrelated and malformed directory names alone.</summary>
        [Fact]
        public void SweepIgnoresUnrelatedDirectories()
        {
            string unrelated = Path.Combine(_root, "something-else");
            string malformed = Path.Combine(_root, "playtest-notanumber-x");
            _ = Directory.CreateDirectory(unrelated);
            _ = Directory.CreateDirectory(malformed);

            PlaytestTempStore.SweepStale(_root, isProcessAlive: _ => false);

            Assert.True(Directory.Exists(unrelated));
            Assert.True(Directory.Exists(malformed));
        }

        /// <summary>Verifies the sweep is safe when the root does not exist yet.</summary>
        [Fact]
        public void SweepOnMissingRootDoesNotThrow()
        {
            PlaytestTempStore.SweepStale(Path.Combine(_root, "absent"), isProcessAlive: _ => false);
        }

        /// <summary>Verifies the default liveness probe recognises the current process as alive.</summary>
        [Fact]
        public void DefaultLivenessProbeSeesCurrentProcessAsAlive()
        {
            string live = Path.Combine(_root, $"playtest-{Environment.ProcessId}-abcdef01");
            _ = Directory.CreateDirectory(live);

            PlaytestTempStore.SweepStale(_root);

            Assert.True(Directory.Exists(live));
        }
    }
}

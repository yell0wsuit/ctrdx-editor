using System;
using System.Threading;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that editor-decoration backgrounds load without deadlocking the calling thread.</summary>
    public class SpriteCacheBackgroundTests
    {
        // Returns bytes only after yielding, so a naive sync-over-async caller would post its
        // continuation to the captured SynchronizationContext and deadlock when that context
        // never pumps (the failure mode on Avalonia's UI thread).
        private sealed class YieldingStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(true);
            }

            public async Task<byte[]> ReadBytesAsync(string relPath)
            {
                await Task.Yield();
                return [];
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult("");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }

            public byte[] ReadBytes(string relPath)
            {
                return [];
            }

            public string ReadText(string relPath)
            {
                return "";
            }
        }

        // Records which read API the cache used. Its async reads never complete synchronously (they
        // model the WebAssembly single thread, where blocking on an async read deadlocks); only the
        // synchronous reads return, so a passing assertion proves the cache took the safe sync path.
        private sealed class RecordingStore : IContentStore
        {
            public bool AsyncBytesCalled;
            public bool SyncBytesCalled;
            public bool SyncTextCalled;
            public string? LastSyncBytesPath;

            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(true);
            }

            public async Task<byte[]> ReadBytesAsync(string relPath)
            {
                AsyncBytesCalled = true;
                await Task.Yield();
                return [];
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult("");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }

            public byte[] ReadBytes(string relPath)
            {
                SyncBytesCalled = true;
                LastSyncBytesPath = relPath;
                return [];
            }

            public string ReadText(string relPath)
            {
                SyncTextCalled = true;
                return "[]";
            }
        }

        /// <summary>
        /// Regression: on-demand background loads must use the store's synchronous read, not sync-over-async.
        /// On single-threaded WebAssembly there is no worker thread, so <c>Task.Run(...).GetResult()</c>
        /// deadlocks the sole UI thread (froze the app when a New Level background/thumbnail was resolved).
        /// </summary>
        [Fact]
        public void GetBackgroundUsesSynchronousStoreRead()
        {
            RecordingStore store = new();
            SpriteCache cache = new(store);

            _ = cache.GetBackground(1);

            Assert.True(store.SyncBytesCalled, "GetBackground must read background bytes synchronously.");
            Assert.False(store.AsyncBytesCalled, "GetBackground must not block on an async read (deadlocks on WASM).");
        }

        /// <summary>
        /// Regression: the background path must honour the configured image extension. The browser bundle
        /// ships WebP, not PNG, so a hardcoded ".png" resolves to no entry and the background renders blank.
        /// </summary>
        [Fact]
        public void GetBackgroundHonoursConfiguredImageExtension()
        {
            RecordingStore store = new();
            SpriteCache cache = new(store, ".webp");

            _ = cache.GetBackground(1);

            Assert.Equal("images/backgrounds/bgr_01_p1.webp", store.LastSyncBytesPath);
        }

        /// <summary>
        /// Regression: on-demand candy-skin loads must use the store's synchronous reads, not sync-over-async,
        /// for the same WebAssembly single-thread reason as <see cref="GetBackgroundUsesSynchronousStoreRead"/>.
        /// </summary>
        [Fact]
        public void GetCandySpriteUsesSynchronousStoreReads()
        {
            RecordingStore store = new();
            SpriteCache cache = new(store);

            _ = cache.GetSprite("candy", candySkin: 1);

            Assert.True(store.SyncBytesCalled, "Loading a candy skin's atlas image must read bytes synchronously.");
            Assert.True(store.SyncTextCalled, "Loading a candy skin's atlas table must read text synchronously.");
            Assert.False(store.AsyncBytesCalled, "Candy-skin loads must not block on an async read (deadlocks on WASM).");
        }

        // A single-threaded context whose posted continuations are never pumped: reproduces the
        // Avalonia UI thread, where blocking on a continuation posted back here deadlocks.
        private sealed class NonPumpingContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback d, object? state)
            {
                // Intentionally never invoked - the owning thread is blocked in GetResult.
            }
        }

        /// <summary>
        /// Regression: GetBackground once blocked the UI thread with sync-over-async file I/O, whose
        /// continuation was posted back to the same blocked thread and deadlocked (froze the app when
        /// the New Level dialog built its background thumbnails). It must return without hanging even
        /// when the calling thread's SynchronizationContext never pumps.
        /// </summary>
        [Fact]
        public void GetBackgroundDoesNotDeadlockUnderSingleThreadedContext()
        {
            SpriteCache cache = new(new YieldingStore());
            using ManualResetEventSlim done = new();

            Thread thread = new(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new NonPumpingContext());
                try
                {
                    // Decode may fail without an initialized render backend; we only assert it returns.
                    _ = cache.GetBackground(1);
                }
                finally
                {
                    done.Set();
                }
            })
            {
                IsBackground = true,
            };
            thread.Start();

            Assert.True(
                done.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
                "GetBackground deadlocked the calling thread.");
        }

        /// <summary>Non-positive ids (Blank/unresolved-Random) resolve to no background bitmap.</summary>
        [Fact]
        public void GetBackgroundReturnsNullForNonPositiveIds()
        {
            SpriteCache cache = new(new YieldingStore());
            Assert.Null(cache.GetBackground(0));
            Assert.Null(cache.GetBackground(-1));
        }
    }
}

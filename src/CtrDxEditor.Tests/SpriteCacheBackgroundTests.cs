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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests sprite cache behavior that does not require an initialized render platform.</summary>
    public class SpriteCachePreloadTests
    {
        private sealed class FakeStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(true);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult<byte[]>([]);
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult(/*lang=json,strict*/ """{"frames":{}}""");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }
        }

        private sealed class ThrowingImageStore : IContentStore
        {
            public readonly List<string> RequestedBytePaths = [];

            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(true);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                RequestedBytePaths.Add(relPath);
                // Stop before SpriteCache ever constructs a Bitmap, which this test project cannot do.
                throw new InvalidOperationException("probe: stop before bitmap decode");
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult(/*lang=json,strict*/ """{"frames":{}}""");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }
        }

        /// <summary>Verifies PreloadAsync appends the provided image extension when reading atlas images.</summary>
        [Fact]
        public async Task PreloadAppendsProvidedImageExtensionWhenReadingAtlasImages()
        {
            ThrowingImageStore store = new();
            SpriteCache cache = new(store, ".webp");

            _ = await Assert.ThrowsAsync<InvalidOperationException>(cache.PreloadAsync);

            Assert.NotEmpty(store.RequestedBytePaths);
            Assert.All(store.RequestedBytePaths, p => Assert.EndsWith(".webp", p));
        }

        /// <summary>Verifies that unknown object elements still return no sprite.</summary>
        [Fact]
        public void GetSpriteReturnsNullForUnknownElement()
        {
            SpriteCache cache = new(new FakeStore());
            Assert.Null(cache.GetSprite("does-not-exist"));
        }
    }
}

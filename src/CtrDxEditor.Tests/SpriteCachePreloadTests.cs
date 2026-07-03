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
                return Task.FromResult("""{"frames":{}}""");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }
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

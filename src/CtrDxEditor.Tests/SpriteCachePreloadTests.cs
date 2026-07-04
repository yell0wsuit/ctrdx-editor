using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Geometry;

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

        /// <summary>Verifies decorative variant layers resolve into sprites separately from base layers.</summary>
        [Fact]
        public void GetSpriteIncludesBubbleOutlineVariants()
        {
            SpriteCache cache = new(new FakeStore());
            SeedBubbleAtlas(cache);

            ObjectSprite sprite = Assert.IsType<ObjectSprite>(cache.GetSprite("bubble"));

            _ = Assert.Single(sprite.Layers);
            Assert.Equal(
                [
                    "obj_bubble_attached_frame_0001.png",
                    "obj_bubble_attached_frame_0002.png",
                    "obj_bubble_attached_frame_0003.png",
                ],
                System.Linq.Enumerable.Select(sprite.Variants, v => v.Frame.Filename));
        }

        private static void SeedBubbleAtlas(SpriteCache cache)
        {
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/obj_bubble.png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/obj_bubble.json"] = new Atlas(
                [
                    Frame("obj_bubble_attached_frame_0000.png"),
                    Frame("obj_bubble_attached_frame_0001.png"),
                    Frame("obj_bubble_attached_frame_0002.png"),
                    Frame("obj_bubble_attached_frame_0003.png"),
                ]),
            });
        }

        private static AtlasFrame Frame(string filename)
        {
            return new AtlasFrame(
                filename,
                new IntRect(0, 0, 1, 1),
                new IntRect(0, 0, 1, 1),
                new IntSize(1, 1),
                Rotated: false,
                Trimmed: false);
        }

        private static void SetPrivateField<T>(SpriteCache cache, string fieldName, T value)
        {
            FieldInfo field = typeof(SpriteCache).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            field.SetValue(cache, value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
                Enumerable.Select(sprite.Variants, v => v.Frame.Filename));
        }

        /// <summary>
        /// Candy sprites resolve frames by quad index, not frame name, so a skin whose atlas names its
        /// frames differently (e.g. "part_1" instead of "frame_08_part_1.png") still composes correctly.
        /// </summary>
        [Fact]
        public void GetSpriteResolvesCandyFramesByQuadIndexRegardlessOfName()
        {
            SpriteCache cache = new(new FakeStore());
            // Deliberately unhelpful frame names ("z0".."z9") that match no descriptor FrameName; only
            // their order matters. Candy = quads 0/1/2, candyL = quad 8, candyR = quad 9.
            SeedDefaultCandyAtlas(cache, [.. Enumerable.Range(0, 10).Select(i => Frame($"z{i}"))]);

            ObjectSprite candy = Assert.IsType<ObjectSprite>(cache.GetSprite("candy"));
            Assert.Equal(["z0", "z1", "z2"], candy.Layers.Select(l => l.Frame.Filename));

            ObjectSprite candyL = Assert.IsType<ObjectSprite>(cache.GetSprite("candyL"));
            Assert.Equal(["z8"], candyL.Layers.Select(l => l.Frame.Filename));

            ObjectSprite candyR = Assert.IsType<ObjectSprite>(cache.GetSprite("candyR"));
            Assert.Equal(["z9"], candyR.Layers.Select(l => l.Frame.Filename));
        }

        /// <summary>The cosmic background earth decoration is game quad 23, not a filename lookup.</summary>
        [Fact]
        public void GetEarthArtResolvesObjStarIdleByQuadIndex()
        {
            SpriteCache cache = new(new FakeStore());
            SeedEarthAtlas(cache);

            SpriteLayerDraw art = Assert.IsType<SpriteLayerDraw>(cache.GetEarthArt());

            Assert.Equal("quad-23-earth", art.Frame.Filename);
        }

        /// <summary>
        /// The target's platform (back) layer is drawn from the selected char_supports frame
        /// (frame_00NN.png), while the character (front) layer stays fixed at char_animations frame_0000.
        /// </summary>
        [Theory]
        [InlineData(0, "frame_0000.png")]
        [InlineData(3, "frame_0003.png")]
        [InlineData(16, "frame_0016.png")]
        public void GetSpriteResolvesTargetPlatformFromSupportFrame(int support, string expectedSupportFrame)
        {
            SpriteCache cache = new(new FakeStore());
            SeedTargetAtlases(cache);

            ObjectSprite target = Assert.IsType<ObjectSprite>(cache.GetSprite("target", candySkin: 0, omNomSupport: support));

            // Layer 0 = platform (char_supports, follows the support), layer 1 = character (fixed).
            Assert.Equal(expectedSupportFrame, target.Layers[0].Frame.Filename);
            Assert.Equal("frame_0000.png", target.Layers[1].Frame.Filename);
        }

        private static void SeedTargetAtlases(SpriteCache cache)
        {
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/char_supports.png"] = bitmap,
                ["images/char_animations.png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/char_supports.json"] = new Atlas([.. Enumerable.Range(0, 17).Select(i => Frame($"frame_{i:D4}.png"))]),
                ["images/char_animations.json"] = new Atlas([Frame("frame_0000.png")]),
            });
        }

        private static void SeedDefaultCandyAtlas(SpriteCache cache, IReadOnlyList<AtlasFrame> frames)
        {
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                [CandySkins.ResourceBase(0) + ".png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                [CandySkins.JsonPath(0)] = new Atlas(frames),
            });
        }

        private static void SeedEarthAtlas(SpriteCache cache)
        {
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            SetPrivateField(cache, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/obj_star_idle.png"] = bitmap,
            });
            SetPrivateField(cache, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/obj_star_idle.json"] = new Atlas(
                    [.. Enumerable.Range(0, 24).Select(i => Frame(i == 23 ? "quad-23-earth" : $"z{i}")),
                     Frame("frame_0058.png")]),
            });
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

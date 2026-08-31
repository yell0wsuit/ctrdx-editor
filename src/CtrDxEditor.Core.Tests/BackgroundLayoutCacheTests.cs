using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests that background layout is recomputed only when its inputs change.</summary>
    public class BackgroundLayoutCacheTests
    {
        /// <summary>Repeated calls with identical inputs compute once.</summary>
        [Fact]
        public void IdenticalInputsComputeOnce()
        {
            BackgroundLayoutCache cache = new();
            int calls = 0;

            BackgroundLayout Compute()
            {
                calls++;
                return Empty();
            }

            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);

            Assert.Equal(1, calls);
        }

        /// <summary>The cached value is returned on a hit, not a default.</summary>
        [Fact]
        public void CachedValueIsReturnedOnHit()
        {
            BackgroundLayoutCache cache = new();
            BackgroundLayout expected = new(12, 34, 56, 78, 1.0, [], null);

            BackgroundLayout first = cache.Get(800, 600, 0, 0.5, 0.25, () => expected);
            BackgroundLayout second = cache.Get(800, 600, 0, 0.5, 0.25, Empty);

            Assert.Equal(expected, first);
            Assert.Equal(expected, second);
        }

        /// <summary>Each input is part of the key, so changing any one forces a recompute.</summary>
        [Theory]
        [InlineData(1024, 600, 0)]
        [InlineData(800, 900, 0)]
        [InlineData(800, 600, 3)]
        public void ChangedInputRecomputes(double width, double height, int background)
        {
            BackgroundLayoutCache cache = new();
            int calls = 0;

            BackgroundLayout Compute()
            {
                calls++;
                return Empty();
            }

            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(width, height, background, 0.5, 0.25, Compute);

            Assert.Equal(2, calls);
        }

        /// <summary>
        /// Changed art aspects recompute even when the level and background index are unchanged, so swapping
        /// the sprite cache mid-session cannot serve a stale layout.
        /// </summary>
        [Theory]
        [InlineData(0.9, 0.25)]
        [InlineData(0.5, 0.8)]
        public void ChangedArtAspectRecomputes(double p1Aspect, double p2Aspect)
        {
            BackgroundLayoutCache cache = new();
            int calls = 0;

            BackgroundLayout Compute()
            {
                calls++;
                return Empty();
            }

            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(800, 600, 0, p1Aspect, p2Aspect, Compute);

            Assert.Equal(2, calls);
        }

        /// <summary>Returning to a previous input set still recomputes; the cache holds one entry, not a map.</summary>
        [Fact]
        public void ReturningToPreviousInputsRecomputes()
        {
            BackgroundLayoutCache cache = new();
            int calls = 0;

            BackgroundLayout Compute()
            {
                calls++;
                return Empty();
            }

            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(1024, 600, 0, 0.5, 0.25, Compute);
            _ = cache.Get(800, 600, 0, 0.5, 0.25, Compute);

            Assert.Equal(3, calls);
        }

        private static BackgroundLayout Empty()
        {
            return new BackgroundLayout(0, 0, 0, 0, 1.0, [], null);
        }
    }
}

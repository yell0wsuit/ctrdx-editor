using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class HitTesterTests
    {
        // Index 0 drawn first (bottom), index 2 drawn last (top).
        private static readonly IReadOnlyList<LevelBounds> Stack =
        [
            new LevelBounds(0, 0, 100, 100),
            new LevelBounds(10, 10, 20, 20),
            new LevelBounds(10, 10, 20, 20),
        ];

        [Fact]
        public void TopmostReturnsHighestIndexContainingPoint()
        {
            Assert.Equal(2, HitTester.Topmost(Stack, new Vec2(15, 15)));
        }

        [Fact]
        public void CyclingPastTopSelectsTheOneUnderneath()
        {
            int next = HitTester.Topmost(Stack, new Vec2(15, 15), afterIndex: 2);

            Assert.Equal(1, next);
        }

        [Fact]
        public void CyclingWrapsBackToTop()
        {
            int next = HitTester.Topmost(Stack, new Vec2(15, 15), afterIndex: 0);

            Assert.Equal(2, next);
        }

        [Fact]
        public void ReturnsMinusOneWhenNothingHit()
        {
            Assert.Equal(-1, HitTester.Topmost(Stack, new Vec2(500, 500)));
        }
    }
}

using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for selecting topmost object bounds under a point.</summary>
    public class HitTesterTests
    {
        // Index 0 drawn first (bottom), index 2 drawn last (top).
        private static readonly IReadOnlyList<LevelBounds> Stack =
        [
            new LevelBounds(0, 0, 100, 100),
            new LevelBounds(10, 10, 20, 20),
            new LevelBounds(10, 10, 20, 20),
        ];

        /// <summary>Verifies that the highest matching index is selected first.</summary>
        [Fact]
        public void TopmostReturnsHighestIndexContainingPoint()
        {
            Assert.Equal(2, HitTester.Topmost(Stack, new Vec2(15, 15)));
        }

        /// <summary>Verifies that cycling below the top hit selects the next object underneath.</summary>
        [Fact]
        public void CyclingPastTopSelectsTheOneUnderneath()
        {
            int next = HitTester.Topmost(Stack, new Vec2(15, 15), afterIndex: 2);

            Assert.Equal(1, next);
        }

        /// <summary>Verifies that hit cycling wraps back to the topmost object.</summary>
        [Fact]
        public void CyclingWrapsBackToTop()
        {
            int next = HitTester.Topmost(Stack, new Vec2(15, 15), afterIndex: 0);

            Assert.Equal(2, next);
        }

        /// <summary>Verifies that a miss returns -1.</summary>
        [Fact]
        public void ReturnsMinusOneWhenNothingHit()
        {
            Assert.Equal(-1, HitTester.Topmost(Stack, new Vec2(500, 500)));
        }
    }
}

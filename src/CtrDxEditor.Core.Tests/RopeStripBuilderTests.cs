using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the game-accurate rope strip builder.</summary>
    public class RopeStripBuilderTests
    {
        /// <summary>With two controls the bezier is a straight lerp.</summary>
        [Fact]
        public void CalcPathBezier_TwoControls_Lerps()
        {
            List<Vec2> controls = [new Vec2(0, 0), new Vec2(10, 20)];
            Vec2 mid = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            Assert.Equal(5, mid.X, 9);
            Assert.Equal(10, mid.Y, 9);
        }

        /// <summary>The curve interpolates the first and last control points exactly.</summary>
        [Fact]
        public void CalcPathBezier_HitsEndpoints()
        {
            List<Vec2> controls = [new Vec2(1, 2), new Vec2(50, 90), new Vec2(-4, 30), new Vec2(7, 8)];
            Vec2 start = RopeStripBuilder.CalcPathBezier(controls, 0);
            Vec2 end = RopeStripBuilder.CalcPathBezier(controls, 1);
            Assert.Equal(new Vec2(1, 2), start);
            Assert.Equal(new Vec2(7, 8), end);
        }

        /// <summary>Symmetric controls give a symmetric curve: the midpoint sits on the axis of symmetry.</summary>
        [Fact]
        public void CalcPathBezier_SymmetricControls_MidpointCentered()
        {
            List<Vec2> controls = [new Vec2(0, 0), new Vec2(50, 100), new Vec2(100, 0)];
            Vec2 mid = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            Assert.Equal(50, mid.X, 9);
            Assert.Equal(50, mid.Y, 9); // quadratic: 0.25*0 + 0.5*100 + 0.25*0
        }
    }
}

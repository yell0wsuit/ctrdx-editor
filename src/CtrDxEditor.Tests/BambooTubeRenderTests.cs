using System;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the bamboo-tube scene integration and its pure hole-geometry bridge.</summary>
    public class BambooTubeRenderTests
    {
        private static readonly Type SceneRenderer =
            typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;

        private static Vec2[] ComputeHoles(double x, double y, double angle)
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "ComputeBambooHoles", BindingFlags.Public | BindingFlags.Static)!;
            return (Vec2[])method.Invoke(null, [new Vec2(x, y), angle])!;
        }

        private static int Layer(string type)
        {
            MethodInfo method = SceneRenderer.GetMethod(
                "GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            return (int)method.Invoke(null, [new LevelObject(new XElement(type))])!;
        }

        /// <summary>
        /// BambooTube draws after the bouncers and before the steam tube, matching GameScene.Draw
        /// (bouncers → bamboo tubes → hands → socks → steam tubes).
        /// </summary>
        [Fact]
        public void BambooTubeDrawsAfterBouncersBeforeSteamTube()
        {
            Assert.True(Layer("bouncer1") <= Layer("pipe"));
            Assert.True(Layer("pipe") <= Layer("steamTube"));
        }

        /// <summary>
        /// At angle 0 the two holes are perpendicular: one straight up, one straight right, each 37.5
        /// game points (12.5 level units at MapScale 3) from the centre — matching UpdateBambooRotation,
        /// which places holes at (x+bb.w/2,y)/(x,y+bb.w/2) then rotates them by (angle − 90°).
        /// </summary>
        [Fact]
        public void HolesArePerpendicularAndEquidistantAtZeroAngle()
        {
            Vec2[] holes = ComputeHoles(100, 100, 0);

            // Hole 0 straight up, hole 1 straight right.
            Assert.Equal(100, holes[0].X, 3);
            Assert.Equal(87.5, holes[0].Y, 3);
            Assert.Equal(112.5, holes[1].X, 3);
            Assert.Equal(100, holes[1].Y, 3);
        }

        /// <summary>Rotating the tube 90° swings the up hole to the right and the right hole downward.</summary>
        [Fact]
        public void HolesRotateWithTheAuthoredAngle()
        {
            Vec2[] holes = ComputeHoles(100, 100, 90);

            Assert.Equal(112.5, holes[0].X, 3);
            Assert.Equal(100, holes[0].Y, 3);
            Assert.Equal(100, holes[1].X, 3);
            Assert.Equal(112.5, holes[1].Y, 3);
        }
    }
}

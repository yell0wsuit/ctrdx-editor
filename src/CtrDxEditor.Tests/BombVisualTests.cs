using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests how the bomb is drawn: its atlas quad, its placement, and where it sits in the z-order.</summary>
    public class BombVisualTests
    {
        private static int DrawLayer(string xml)
        {
            MethodInfo method = typeof(VisualDescriptorMap).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!
                .GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            return (int)method.Invoke(null, [new LevelObject(XElement.Parse(xml))])!;
        }

        /// <summary>The bomb draws its intact body quad, centered on the bomb as Bomb's body sprite is.</summary>
        [Fact]
        public void BombUsesItsBodyQuadCenteredOnTheFrame()
        {
            VisualDescriptor bomb = VisualDescriptorMap.For("bomb")!;

            SpriteLayer body = Assert.Single(bomb.Layers);
            Assert.Equal(0, body.Quad);
            Assert.Equal("images/obj_bomb.json", body.AtlasJsonRelPath);
            Assert.True(body.CenterOnFrame);
        }

        /// <summary>The bomb's atlas ships in the asset bundle, so it is required like the other Time Travel art.</summary>
        [Fact]
        public void BombArtIsRequiredOfTheBundle()
        {
            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");

            Assert.Contains("images/obj_bomb.json", required);
            Assert.Contains("images/obj_bomb.webp", required);
        }

        /// <summary>A bomb draws with the candies, which is the list the game keeps it in.</summary>
        [Fact]
        public void BombDrawsOnTheCandyLayer()
        {
            Assert.Equal(DrawLayer("""<candy x="1" y="1" />"""), DrawLayer("""<bomb x="1" y="1" />"""));
        }
    }
}

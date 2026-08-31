using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests how the axe is drawn: the atlas quads it composites, and where it sits in the z-order.</summary>
    public class AxeVisualTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        private static int DrawLayer(LevelObject obj)
        {
            MethodInfo method = typeof(VisualDescriptorMap).Assembly
                .GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!
                .GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            return (int)method.Invoke(null, [obj])!;
        }

        /// <summary>The axe composites the game's base, blade, and pivot quads in that order.</summary>
        [Fact]
        public void AxeUsesBaseBladeAndPivotQuads()
        {
            VisualDescriptor axe = VisualDescriptorMap.For("axe")!;

            Assert.Equal([0, 1, 2], axe.Layers.Select(l => l.Quad));
            Assert.All(axe.Layers, l => Assert.Equal("images/obj_axe.json", l.AtlasJsonRelPath));

            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_axe.json", required);
            Assert.Contains("images/obj_axe.webp", required);
        }

        /// <summary>An axe draws with the candies, which is the list the game keeps it in.</summary>
        [Fact]
        public void AxeDrawsOnTheCandyLayer()
        {
            Assert.Equal(
                DrawLayer(Obj("""<candy x="1" y="1" />""")),
                DrawLayer(Obj("""<axe x="1" y="1" />""")));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests how a chain rope is drawn: the atlas quads its links come from, and the chain-anchor hook
    /// art a chain grab picks up in place of its ordinary hook.
    /// </summary>
    public class ChainVisualTests
    {
        private static LevelObject Obj(string xml)
        {
            return new(XElement.Parse(xml));
        }

        private static string SpriteKey(LevelObject obj)
        {
            MethodInfo method = typeof(VisualDescriptorMap).Assembly
                .GetType("CtrDxEditor.Rendering.GrabRenderer")!
                .GetMethod("SpriteKey", BindingFlags.Public | BindingFlags.Static)!;
            return (string)method.Invoke(null, [obj])!;
        }

        /// <summary>The chain draws from the two quads of the game's chain atlas.</summary>
        [Fact]
        public void ChainLinksUseTheChainAtlasQuads()
        {
            Assert.Equal(0, Assert.Single(VisualDescriptorMap.For("chain_link")!.Layers).Quad);
            Assert.Equal(1, Assert.Single(VisualDescriptorMap.For("chain_mid")!.Layers).Quad);
            Assert.All(
                VisualDescriptorMap.For("chain_link")!.Layers,
                l => Assert.Equal("images/obj_exp_chain.json", l.AtlasJsonRelPath));

            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_exp_chain.json", required);
            Assert.Contains("images/obj_exp_chain.webp", required);
        }

        /// <summary>The chain hook variants come from their own atlases, back then front.</summary>
        [Fact]
        public void ChainHookVariantsUseTheirOwnAtlases()
        {
            Assert.Equal([0, 1], VisualDescriptorMap.For("grab_chain")!.Layers.Select(l => l.Quad));
            Assert.All(
                VisualDescriptorMap.For("grab_chain")!.Layers,
                l => Assert.Equal("images/obj_hook_chain.json", l.AtlasJsonRelPath));

            Assert.Equal([0, 1], VisualDescriptorMap.For("grab_auto_chain")!.Layers.Select(l => l.Quad));
            Assert.All(
                VisualDescriptorMap.For("grab_auto_chain")!.Layers,
                l => Assert.Equal("images/obj_hook_auto_chain.json", l.AtlasJsonRelPath));
        }

        /// <summary>A chain grab swaps its hook for the chain anchor art, plain or auto-catch.</summary>
        [Theory]
        [InlineData("""<grab x="1" y="1" length="50" breakable="false" />""", "grab_chain")]
        [InlineData("""<grab x="1" y="1" radius="100" breakable="false" />""", "grab_auto_chain")]
        [InlineData("""<grab x="1" y="1" length="50" />""", "grab")]
        [InlineData("""<grab x="1" y="1" radius="100" />""", "grab_auto")]
        public void ChainGrabsPickTheChainHookArt(string xml, string expected)
        {
            Assert.Equal(expected, SpriteKey(Obj(xml)));
        }

        /// <summary>
        /// A gun, wheel, or suction cup draws its own art whatever the rope is - the game's
        /// CreateAxisVisuals never reaches the chain branch for those.
        /// </summary>
        [Theory]
        [InlineData("""<grab x="1" y="1" gun="true" breakable="false" />""", "grab_gun")]
        [InlineData("""<grab x="1" y="1" wheel="true" breakable="false" />""", "grab_wheel")]
        [InlineData("""<grab x="1" y="1" kickable="true" breakable="false" />""", "grab_suction")]
        public void ChainDoesNotChangeGunWheelOrSuctionArt(string xml, string expected)
        {
            Assert.Equal(expected, SpriteKey(Obj(xml)));
        }
    }
}

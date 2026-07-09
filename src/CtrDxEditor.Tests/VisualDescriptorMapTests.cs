using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for the built-in visual descriptor set.</summary>
    public class VisualDescriptorMapTests
    {
        private static readonly string[] ElectroSpriteKeys =
        [
            "electro",
            "electro_off",
            "electro_on_1",
            "electro_on_2",
            "electro_on_3",
            "electro_on_4",
        ];

        /// <summary>
        /// Verifies the bubble draws the game's quad 0 attached frame over one of the three random
        /// attached-outline variants used by LoadBubble.
        /// </summary>
        [Fact]
        public void BubbleHasAttachedBaseAndThreeOutlineVariants()
        {
            VisualDescriptor bubble = VisualDescriptorMap.For("bubble")!;

            SpriteLayer baseLayer = Assert.Single(bubble.Layers);
            Assert.Equal("images/obj_bubble.json", baseLayer.AtlasJsonRelPath);
            Assert.Equal(0, baseLayer.Quad);

            Assert.Equal([1, 2, 3], bubble.RandomBackLayers.Select(l => l.Quad));
        }

        /// <summary>Verifies hook-family descriptors resolve the engine quad positions, not frame names.</summary>
        [Fact]
        public void HookFamilyDescriptorsUseGameQuadIndices()
        {
            Assert.Equal([0, 1], VisualDescriptorMap.For("grab")!.Layers.Select(l => l.Quad));
            Assert.Equal([4, 5], VisualDescriptorMap.For("grab_auto")!.Layers.Select(l => l.Quad));
            Assert.Equal([6, 8, 7], VisualDescriptorMap.For("grab_rail")!.Layers.Select(l => l.Quad));
            Assert.Equal([10], VisualDescriptorMap.For("grab_movable")!.Layers.Select(l => l.Quad));
            Assert.Equal([9], VisualDescriptorMap.For("grab_movable_highlight")!.Layers.Select(l => l.Quad));
        }

        /// <summary>Verifies simple zero-based atlases carry their quad indices in descriptors.</summary>
        [Fact]
        public void SingleAtlasObjectsUseQuadIndices()
        {
            Assert.Equal([0, 1, 2], VisualDescriptorMap.For("grab_gun")!.Layers.Select(l => l.Quad));
            Assert.Equal([0], VisualDescriptorMap.For("grab_spider")!.Layers.Select(l => l.Quad));
            Assert.Equal([3, 4], VisualDescriptorMap.For("grab_suction")!.Layers.Select(l => l.Quad));
            Assert.Equal([1, 2], VisualDescriptorMap.For("grab_suction_kicked")!.Layers.Select(l => l.Quad));
            Assert.Equal([0, 18], VisualDescriptorMap.For("star")!.Layers.Select(l => l.Quad));
            Assert.Equal([20, 19], VisualDescriptorMap.For("star_timed")!.Layers.Select(l => l.Quad));
        }

        /// <summary>Night stars are not registered as a separate static editor sprite.</summary>
        [Fact]
        public void NightStarDescriptorIsNotRegistered()
        {
            Assert.Null(VisualDescriptorMap.For("star_night"));
        }

        /// <summary>Night Om Nom uses the classic sleeping spritesheet and keeps the support layer.</summary>
        [Fact]
        public void SleepingTargetUsesSupportAndClassicSleepingSprite()
        {
            VisualDescriptor target = VisualDescriptorMap.For("target_sleeping")!;

            Assert.Equal(
                ["images/char_supports.json", "images/char_animations_sleeping.json"],
                target.Layers.Select(l => l.AtlasJsonRelPath));
            Assert.Equal([0, 6], target.Layers.Select(l => l.Quad));
        }

        /// <summary>Verifies random back layers count toward the files a content bundle must provide.</summary>
        [Fact]
        public void RequiredFilesCoverRandomBackLayerAtlases()
        {
            IReadOnlyCollection<string> required =
                VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_bubble.webp", required);
            Assert.Contains("images/obj_bubble.json", required);
        }

        /// <summary>Verifies that gravity switches use the same static quad as the in-game toggle button.</summary>
        [Fact]
        public void GravitySwitchUsesObjStarIdleButtonFrame()
        {
            VisualDescriptor gravitySwitch = VisualDescriptorMap.For("gravitySwitch")!;

            SpriteLayer layer = Assert.Single(gravitySwitch.Layers);
            Assert.Equal("images/obj_star_idle.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_star_idle", layer.AtlasImageBasePath);
            Assert.Equal(21, layer.Quad);
        }

        /// <summary>The additive lit-glow halo is registered as its own quad, separate from the bulb sprite.</summary>
        [Fact]
        public void LightBulbGlowUsesLightQuad()
        {
            VisualDescriptor glow = VisualDescriptorMap.For("lightBulb_glow")!;

            SpriteLayer layer = Assert.Single(glow.Layers);
            Assert.Equal("images/obj_lighter.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_lighter", layer.AtlasImageBasePath);
            Assert.Equal(0, layer.Quad);
        }

        /// <summary>Verifies the pump maps to a single resting-quad layer over the pump atlas.</summary>
        [Fact]
        public void PumpMapsToRestingQuadOfPumpAtlas()
        {
            VisualDescriptor pump = VisualDescriptorMap.For("pump")!;
            SpriteLayer layer = Assert.Single(pump.Layers);
            Assert.Equal("images/obj_pump.json", layer.AtlasJsonRelPath);
            Assert.Equal("images/obj_pump", layer.AtlasImageBasePath);
            Assert.Equal(0, layer.Quad);
        }

        /// <summary>Verifies static spikes use obj_spikes quads 8-11 and rotatable spikes use quads 0-3 plus their group buttons.</summary>
        [Theory]
        [InlineData("spike1", 8, 0, 4, 6)]
        [InlineData("spike2", 9, 1, 4, 6)]
        [InlineData("spike3", 10, 2, 4, 6)]
        [InlineData("spike4", 11, 3, 4, 6)]
        public void SpikeDescriptorsMapToStaticRotatableAndButtonQuads(
            string element,
            int staticQuad,
            int rotatableQuad,
            int group1ButtonQuad,
            int group2ButtonQuad)
        {
            VisualDescriptor spike = VisualDescriptorMap.For(element)!;
            VisualDescriptor group0 = VisualDescriptorMap.For($"{element}_toggled_0")!;
            VisualDescriptor group1 = VisualDescriptorMap.For($"{element}_toggled_1")!;
            VisualDescriptor group2 = VisualDescriptorMap.For($"{element}_toggled_2")!;

            Assert.Equal([staticQuad], spike.Layers.Select(l => l.Quad));
            Assert.Equal([rotatableQuad], group0.Layers.Select(l => l.Quad));
            Assert.Equal([rotatableQuad, group1ButtonQuad], group1.Layers.Select(l => l.Quad));
            Assert.Equal([rotatableQuad, group2ButtonQuad], group2.Layers.Select(l => l.Quad));
            Assert.All(spike.Layers.Concat(group0.Layers).Concat(group1.Layers).Concat(group2.Layers), l =>
            {
                Assert.Equal("images/obj_spikes.json", l.AtlasJsonRelPath);
                Assert.Equal("images/obj_spikes", l.AtlasImageBasePath);
            });
        }

        /// <summary>Electro descriptors expose the base/off frame and the four electric loop frames.</summary>
        [Fact]
        public void ElectroDescriptorsMapToOffAndOnAnimationQuads()
        {
            Assert.Equal([0], VisualDescriptorMap.For("electro_off")!.Layers.Select(l => l.Quad));
            Assert.Equal([1], VisualDescriptorMap.For("electro_on_1")!.Layers.Select(l => l.Quad));
            Assert.Equal([2], VisualDescriptorMap.For("electro_on_2")!.Layers.Select(l => l.Quad));
            Assert.Equal([3], VisualDescriptorMap.For("electro_on_3")!.Layers.Select(l => l.Quad));
            Assert.Equal([4], VisualDescriptorMap.For("electro_on_4")!.Layers.Select(l => l.Quad));

            VisualDescriptor electro = VisualDescriptorMap.For("electro")!;
            Assert.Equal([0], electro.Layers.Select(l => l.Quad));
            Assert.All(
                ElectroSpriteKeys.SelectMany(k => VisualDescriptorMap.For(k)!.Layers),
                l =>
                {
                    Assert.Equal("images/obj_electrodes.json", l.AtlasJsonRelPath);
                    Assert.Equal("images/obj_electrodes", l.AtlasImageBasePath);
                });
        }
    }
}

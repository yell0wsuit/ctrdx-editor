using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests how the pause switcher is drawn: its atlas quad, and where it sits in the z-order.</summary>
    public class PauseSwitcherVisualTests
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

        /// <summary>The pause switcher shows the running face a level starts on.</summary>
        [Fact]
        public void PauseSwitcherUsesTheRunningFaceQuad()
        {
            VisualDescriptor switcher = VisualDescriptorMap.For("pauseSwitcher")!;

            SpriteLayer face = Assert.Single(switcher.Layers);
            Assert.Equal(0, face.Quad);
            Assert.Equal("images/obj_pause.json", face.AtlasJsonRelPath);

            IReadOnlyCollection<string> required = VisualDescriptorMap.RequiredFiles(".webp");
            Assert.Contains("images/obj_pause.json", required);
            Assert.Contains("images/obj_pause.webp", required);
        }

        /// <summary>The pause switcher draws between the spikes and the bouncers, as the game does.</summary>
        [Fact]
        public void PauseSwitcherDrawsBetweenSpikesAndBouncers()
        {
            int spikes = DrawLayer(Obj("""<spike1 x="1" y="1" />"""));
            int switcher = DrawLayer(Obj("""<pauseSwitcher x="1" y="1" />"""));
            int bouncers = DrawLayer(Obj("""<bouncer1 x="1" y="1" />"""));

            Assert.True(switcher > spikes);
            Assert.True(switcher < bouncers);
        }
    }
}

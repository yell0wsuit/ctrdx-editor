using System;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests lantern draw-order classification.</summary>
    public class LanternRenderTests
    {
        /// <summary>Lanterns share the candy draw layer in the game's fixed z-order.</summary>
        [Fact]
        public void LanternDrawsOnTheCandyLayer()
        {
            LevelObject lantern = new(XElement.Parse("""<lantern x="1" y="1" candyCaptured="true" />"""));
            Type? renderer = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer");
            Assert.NotNull(renderer);
            MethodInfo? method = renderer.GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            Assert.Equal(12, method.Invoke(null, [lantern]));
        }
    }
}

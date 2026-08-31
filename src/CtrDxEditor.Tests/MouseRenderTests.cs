using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests mouse (gap) draw-order classification.</summary>
    public class MouseRenderTests
    {
        /// <summary>
        /// Mice bodies draw via GameScene.Draw's DrawMice() call, after the bouncers and just before
        /// the socks, so the editor slots the gap on the sock tier (layer 7): above bouncers, below
        /// steam pipes, ghosts, grabs, and candy.
        /// </summary>
        [Fact]
        public void GapDrawsOnTheSockTier()
        {
            LevelObject gap = new(XElement.Parse("""<gap x="1" y="1" angle="0" radius="50" activeTime="1.0" index="1" />"""));
            Type? renderer = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer");
            Assert.NotNull(renderer);
            MethodInfo? method = renderer.GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            Assert.Equal(8, method.Invoke(null, [gap]));
        }

        /// <summary>The mouse's activation index is drawn on it as an on-canvas label.</summary>
        [Fact]
        public void GapShowsItsIndexAsALabel()
        {
            LevelObject gap = new(XElement.Parse("""<gap x="1" y="1" index="3" />"""));

            Assert.Equal("3", BindingIdLabel(gap, [gap]));
        }

        /// <summary>A mouse without an index shows no label.</summary>
        [Fact]
        public void GapWithoutIndexShowsNoLabel()
        {
            LevelObject gap = new(XElement.Parse("""<gap x="1" y="1" />"""));

            Assert.Null(BindingIdLabel(gap, [gap]));
        }

        private static string? BindingIdLabel(LevelObject obj, IReadOnlyList<LevelObject> objects)
        {
            Type renderer = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo method = renderer.GetMethod("BindingIdLabel", BindingFlags.NonPublic | BindingFlags.Static)!;
            return (string?)method.Invoke(null, [obj, objects]);
        }
    }
}

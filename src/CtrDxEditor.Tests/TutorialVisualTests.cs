using System;
using System.Reflection;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tutorial icon sprite layers map each tag to its tutorial_signs quad.</summary>
    public class TutorialVisualTests
    {
        /// <summary>Maps each tutorial icon tag to the corresponding atlas quad.</summary>
        [Fact]
        public void EachIconTagMapsToItsQuad()
        {
            for (int q = 0; q < TutorialObject.IconCount; q++)
            {
                VisualDescriptor? v = VisualDescriptorMap.For(TutorialObject.TagForQuad(q));
                Assert.NotNull(v);
                SpriteLayer layer = Assert.Single(v!.Layers);
                Assert.Equal("images/tutorial_signs.json", layer.AtlasJsonRelPath);
                Assert.Equal(q, layer.Quad);
            }
        }

        /// <summary>Includes the tutorial signs atlas in required content files.</summary>
        [Fact]
        public void RequiredFilesIncludeTutorialSigns()
        {
            Assert.Contains("images/tutorial_signs.png", VisualDescriptorMap.RequiredFiles(".png"));
        }

        /// <summary>Draws tutorial text above gameplay objects and tutorial icons above tutorial text.</summary>
        [Fact]
        public void TutorialsDrawOnTopTextThenIcons()
        {
            LevelObject text = new(new System.Xml.Linq.XElement("tutorialText"));
            LevelObject icon = new(new System.Xml.Linq.XElement("tutorial04"));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo gameDrawLayer = renderer.GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            int textLayer = (int)gameDrawLayer.Invoke(null, [text])!;
            int iconLayer = (int)gameDrawLayer.Invoke(null, [icon])!;
            Assert.Equal(14, textLayer);
            Assert.Equal(15, iconLayer);
            Assert.True(iconLayer > textLayer);
        }
    }
}

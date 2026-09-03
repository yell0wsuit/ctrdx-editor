using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

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
                SpriteLayer layer = Assert.Single(v.Layers);
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
            LevelObject text = new(new XElement("tutorialText"));
            LevelObject icon = new(new XElement("tutorial04"));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo gameDrawLayer = renderer.GetMethod("GameDrawLayer", BindingFlags.Public | BindingFlags.Static)!;
            int textLayer = (int)gameDrawLayer.Invoke(null, [text])!;
            int iconLayer = (int)gameDrawLayer.Invoke(null, [icon])!;
            Assert.Equal(15, textLayer);
            Assert.Equal(16, iconLayer);
            Assert.True(iconLayer > textLayer);
        }

        /// <summary>Uses byte-domain translation values so black line art inverts to white.</summary>
        [Fact]
        public void InvertMatrixMapsBlackToWhite()
        {
            Type operation = typeof(VisualDescriptorMap).Assembly.GetType(
                "CtrDxEditor.Rendering.TutorialInvertDrawOperation")!;
            FieldInfo matrixField = operation.GetField("InvertMatrix", BindingFlags.NonPublic | BindingFlags.Static)!;
            float[] matrix = (float[])matrixField.GetValue(null)!;

            Assert.Equal(255, matrix[4]);
            Assert.Equal(255, matrix[9]);
            Assert.Equal(255, matrix[14]);
        }

        /// <summary>Palette drag ghosts use the tutorial renderer so dark-theme inversion is preserved.</summary>
        [Fact]
        public void PaletteDragPreviewUsesTutorialIconRenderer()
        {
            string source = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared",
                "Rendering",
                "LevelCanvas.Rendering.cs"));

            Assert.Contains("TutorialObject.IsImage(dragPreviewElement)", source, StringComparison.Ordinal);
            Assert.Contains("TutorialRenderer.DrawIcon(", source, StringComparison.Ordinal);
        }

        /// <summary>Expands tutorial text selection bounds to contain wrapped lines.</summary>
        [Fact]
        public void WrappedTextSelectionBoundsIncludeEveryLine()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            LevelObject text = new(new XElement(
                "tutorialText",
                new XAttribute("x", "100"),
                new XAttribute("y", "100"),
                new XAttribute("text", "aaa bbb ccc"),
                new XAttribute("width", "20")));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo selectionBounds = renderer.GetMethod("SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;

            LevelBounds bounds = (LevelBounds)selectionBounds.Invoke(null, [sprites, text, 0, 0, false])!;

            Assert.True(bounds.H > 50);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                    ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }

        /// <summary>Treats tutorial text coordinates as the game's top-left wrap-box origin.</summary>
        [Fact]
        public void TextSelectionBoundsStartAtAuthoredPosition()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            LevelObject text = new(new XElement(
                "tutorialText",
                new XAttribute("x", "100"),
                new XAttribute("y", "120"),
                new XAttribute("text", "Tutorial"),
                new XAttribute("width", "140")));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo selectionBounds = renderer.GetMethod("SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;

            LevelBounds bounds = (LevelBounds)selectionBounds.Invoke(null, [sprites, text, 0, 0, false])!;

            Assert.Equal(100, bounds.X, precision: 9);
            Assert.Equal(120, bounds.Y, precision: 9);
            Assert.Equal(140, bounds.W, precision: 9);
        }

        /// <summary>Centers the visible spriteSourceSize region on the authored tutorial position.</summary>
        [Fact]
        public void IconSelectionBoundsCenterTrimmedTutorialArtOnAnchor()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            AtlasFrame frame = new(
                "frame_0000.png",
                new IntRect(1, 933, 150, 14),
                new IntRect(584, 766, 150, 14),
                new IntSize(998, 1058),
                Rotated: false,
                Trimmed: true);
            SetPrivateField(sprites, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/tutorial_signs.png"] = bitmap,
            });
            SetPrivateField(sprites, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/tutorial_signs.json"] = new Atlas([frame]),
            });
            LevelObject icon = new(new XElement(
                "tutorial01",
                new XAttribute("x", "100"),
                new XAttribute("y", "100")));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo selectionBounds = renderer.GetMethod("SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;

            LevelBounds actual = (LevelBounds)selectionBounds.Invoke(null, [sprites, icon, 0, 0, false])!;
            Assert.Equal(icon.X, actual.X + (actual.W / 2), precision: 9);
            Assert.Equal(icon.Y, actual.Y + (actual.H / 2), precision: 9);
            Assert.Equal(150.0 / SpritePlacement.MapScale, actual.W, precision: 9);
        }

        /// <summary>Keeps very thin tutorial art practical to click without using the huge sourceSize canvas.</summary>
        [Fact]
        public void ThinIconSelectionBoundsHaveMinimumHeight()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            Bitmap bitmap = (Bitmap)RuntimeHelpers.GetUninitializedObject(typeof(Bitmap));
            AtlasFrame frame = new(
                "frame_0000.png",
                new IntRect(1, 933, 150, 14),
                new IntRect(584, 766, 150, 14),
                new IntSize(998, 1058),
                Rotated: false,
                Trimmed: true);
            SetPrivateField(sprites, "_bitmaps", new Dictionary<string, Bitmap>
            {
                ["images/tutorial_signs.png"] = bitmap,
            });
            SetPrivateField(sprites, "_atlases", new Dictionary<string, Atlas>
            {
                ["images/tutorial_signs.json"] = new Atlas([frame]),
            });
            LevelObject icon = new(new XElement(
                "tutorial01",
                new XAttribute("x", "100"),
                new XAttribute("y", "100")));
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.LevelSceneRenderer")!;
            MethodInfo selectionBounds = renderer.GetMethod("SelectionBounds", BindingFlags.Public | BindingFlags.Static)!;

            LevelBounds actual = (LevelBounds)selectionBounds.Invoke(null, [sprites, icon, 0, 0, false])!;

            Assert.Equal(16, actual.H, precision: 9);
            Assert.Equal(icon.Y, actual.Y + (actual.H / 2), precision: 9);
        }

        /// <summary>
        /// Authored <c>size</c> scales the font's own height but not the fixed top spacing, matching the
        /// game's <c>GetTopSpacing()</c>, which is added after the size multiplier rather than scaled by
        /// it. Isolating the delta (rather than asserting the whole formula) still fails if size scaling
        /// is missing, applied to the wrong term, or applied non-linearly.
        /// </summary>
        [Fact]
        public void TextSelectionHeightGrowsByExactlyOneFontSizeWhenSizeDoubles()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            LevelObject unscaled = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Hi"),
                new XAttribute("width", "1000")));
            LevelObject scaled = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Hi"),
                new XAttribute("width", "1000"),
                new XAttribute("size", "2")));
            MethodInfo textBounds = TutorialRendererMethod("TextBounds");
            double fontSizeLevel = (double)TutorialFontField("FontSizeLevel").GetValue(null)!;

            LevelBounds unscaledBounds = (LevelBounds)textBounds.Invoke(null, [sprites, unscaled])!;
            LevelBounds scaledBounds = (LevelBounds)textBounds.Invoke(null, [sprites, scaled])!;

            Assert.Equal(fontSizeLevel, scaledBounds.H - unscaledBounds.H, precision: 6);
        }

        /// <summary>
        /// Authored <c>lineHeight</c> scales the gap between wrapped lines. The text is forced to wrap
        /// to exactly two lines by a width narrower than either word, so the delta isolates the single
        /// inter-line gap the multiplier controls, independent of the font actually resolved.
        /// </summary>
        [Fact]
        public void TextSelectionHeightGrowsByLineAdvanceTimesLineHeightDelta()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            LevelObject oneLineHeight = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Hi There"),
                new XAttribute("width", "1")));
            LevelObject tripleLineHeight = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Hi There"),
                new XAttribute("width", "1"),
                new XAttribute("lineHeight", "3")));
            MethodInfo textBounds = TutorialRendererMethod("TextBounds");
            double lineAdvanceLevel = (double)TutorialFontField("LineAdvanceLevel").GetValue(null)!;

            LevelBounds baseline = (LevelBounds)textBounds.Invoke(null, [sprites, oneLineHeight])!;
            LevelBounds tripled = (LevelBounds)textBounds.Invoke(null, [sprites, tripleLineHeight])!;

            // A width of 1 forces "Hi" and "There" onto separate lines regardless of the resolved font,
            // since Wrap never splits a single word and always starts a line with at least one word.
            Assert.Equal(2 * lineAdvanceLevel, tripled.H - baseline.H, precision: 6);
        }

        /// <summary>
        /// Auto-width measures at the authored size, so a bigger face gets a wider box for the same text
        /// rather than wrapping against a box sized for the default scale.
        /// </summary>
        [Fact]
        public void AutoWidthScalesWithAuthoredSize()
        {
            SpriteCache sprites = new(new EmptyContentStore());
            LevelObject unscaled = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Om Nom")));
            LevelObject scaled = new(new XElement(
                "tutorialText",
                new XAttribute("x", "0"),
                new XAttribute("y", "0"),
                new XAttribute("text", "Om Nom"),
                new XAttribute("size", "2")));
            TutorialObject.SetAutoWidth(unscaled, true);
            TutorialObject.SetAutoWidth(scaled, true);
            MethodInfo applyAutoWidth = TutorialRendererMethod("ApplyAutoWidth");

            _ = applyAutoWidth.Invoke(null, [sprites, unscaled]);
            _ = applyAutoWidth.Invoke(null, [sprites, scaled]);

            double unscaledWidth = double.Parse(unscaled.GetAttr("width")!, CultureInfo.InvariantCulture);
            double scaledWidth = double.Parse(scaled.GetAttr("width")!, CultureInfo.InvariantCulture);
            double ratio = scaledWidth / unscaledWidth;

            // Not exactly 2x: both widths are rounded up to a whole level unit by ApplyAutoWidth.
            Assert.InRange(ratio, 1.8, 2.2);
        }

        private static MethodInfo TutorialRendererMethod(string name)
        {
            Type renderer = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.TutorialRenderer")!;
            return renderer.GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
        }

        private static FieldInfo TutorialFontField(string name)
        {
            Type font = typeof(VisualDescriptorMap).Assembly.GetType("CtrDxEditor.Rendering.TutorialFont")!;
            return font.GetField(name, BindingFlags.Public | BindingFlags.Static)!;
        }

        private static void SetPrivateField<T>(SpriteCache cache, string name, T value)
        {
            typeof(SpriteCache).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cache, value);
        }
    }
}

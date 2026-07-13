using System;
using System.IO;
using System.Reflection;

using CtrDxEditor.Content;

using SkiaSharp;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the tutorial font used when the game font is unavailable.</summary>
    public class TutorialFontTests
    {
        /// <summary>The editor fallback is explicitly Inter on every platform.</summary>
        [Fact]
        public void DefaultTypefaceIsInter()
        {
            Type tutorialFont = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.TutorialFont")!;
            MethodInfo resolveDefaultTypeface = tutorialFont.GetMethod(
                "ResolveDefaultTypeface",
                BindingFlags.NonPublic | BindingFlags.Static,
                Type.EmptyTypes)!;

            using SKTypeface typeface = Assert.IsType<SKTypeface>(
                resolveDefaultTypeface.Invoke(null, null),
                exactMatch: false);

            Assert.Equal("Inter", typeface.FamilyName);
        }

        /// <summary>A missing required Inter asset must not silently select a platform font.</summary>
        [Fact]
        public void MissingInterTypefaceThrows()
        {
            Type tutorialFont = typeof(SpriteCache).Assembly.GetType("CtrDxEditor.Rendering.TutorialFont")!;
            MethodInfo resolveDefaultTypeface = tutorialFont.GetMethod(
                "ResolveDefaultTypeface",
                BindingFlags.NonPublic | BindingFlags.Static,
                [typeof(Func<Stream>)])!;

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                _ = resolveDefaultTypeface.Invoke(
                    null,
                    [new Func<Stream>(() => throw new FileNotFoundException("Inter asset missing"))]));

            _ = Assert.IsType<FileNotFoundException>(exception.InnerException);
        }
    }
}

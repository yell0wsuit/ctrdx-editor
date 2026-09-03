using System;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Tutorial-icon (<c>tutorial01</c>–<c>tutorial11</c>) and tutorial-text (<c>tutorialText</c>)
    /// helpers. The icon quad lives only in the tag name, so changing icon renames the element.
    /// Quads 9 (finger) and 10 (fingers) are full-color and are never inverted on the dark canvas.
    /// </summary>
    public static class TutorialObject
    {
        private sealed class AutoWidthState;

        /// <summary>Number of tutorial icon quads / tags.</summary>
        public const int IconCount = 11;

        /// <summary>Element placed for a new tutorial icon.</summary>
        public const string DefaultElement = "tutorial01";

        /// <summary>Default wrap width for a new tutorial text.</summary>
        public const int DefaultTextWidth = 140;

        /// <summary>Placeholder text so a freshly placed tutorial text is visible.</summary>
        public const string DefaultText = "Text";

        /// <summary>The tutorial-text element name.</summary>
        public const string TextElement = "tutorialText";

        /// <summary>Whether the tutorial text auto-sizes its width to the text (default for new text).</summary>
        public static bool IsAutoWidth(LevelObject o)
        {
            return o.Element.Annotation<AutoWidthState>() is not null;
        }

        /// <summary>Turns editor-only auto-width state on or off without changing serialized level XML.</summary>
        /// <param name="o">The tutorial text object.</param>
        /// <param name="auto">True to auto-size the width to the text; false to keep the authored width.</param>
        public static void SetAutoWidth(LevelObject o, bool auto)
        {
            o.Element.RemoveAnnotations<AutoWidthState>();
            o.RemoveAttr("autoWidth"); // Clean up levels touched by pre-release editor builds.
            if (auto)
            {
                o.Element.AddAnnotation(new AutoWidthState());
            }
        }

        /// <summary>Whether <paramref name="type"/> is one of the 11 tutorial icon tags.</summary>
        public static bool IsImage(string type)
        {
            return QuadForTag(type) >= 0;
        }

        /// <summary>Whether <paramref name="type"/> is the tutorial text tag.</summary>
        public static bool IsText(string type)
        {
            return type == TextElement;
        }

        /// <summary>The zero-based quad for a <c>tutorialNN</c> tag, or -1 when not an icon tag.</summary>
        public static int QuadForTag(string type)
        {
            if (type is not { Length: 10 } || !type.StartsWith("tutorial", StringComparison.Ordinal))
            {
                return -1;
            }

            if (!int.TryParse(type.AsSpan(8), NumberStyles.None, CultureInfo.InvariantCulture, out int nn))
            {
                return -1;
            }

            int quad = nn - 1;
            return quad is >= 0 and < IconCount ? quad : -1;
        }

        /// <summary>The <c>tutorialNN</c> tag for a zero-based quad.</summary>
        public static string TagForQuad(int quad)
        {
            return "tutorial" + (quad + 1).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>The object's current icon quad.</summary>
        public static int Icon(LevelObject o)
        {
            return QuadForTag(o.Type);
        }

        /// <summary>Renames the element to the tag for <paramref name="quad"/>, preserving attributes.</summary>
        /// <param name="o">The tutorial icon object.</param>
        /// <param name="quad">The zero-based icon quad.</param>
        public static void SetIcon(LevelObject o, int quad)
        {
            o.Element.Name = TagForQuad(quad);
        }

        /// <summary>Whether a quad is one of the two full-color icons (finger, fingers).</summary>
        public static bool IsColoredQuad(int quad)
        {
            return quad is 9 or 10;
        }

        /// <summary>Whether an icon quad should be color-inverted for the current canvas.</summary>
        /// <param name="quad">The zero-based icon quad.</param>
        /// <param name="dark">Whether the canvas is dark. Full-color icons never invert, so only monochrome art flips.</param>
        public static bool ShouldInvert(int quad, bool dark)
        {
            return dark && !IsColoredQuad(quad);
        }

        /// <summary>
        /// Whether an icon quad should be drawn through ink replacement rather than as its unmodified
        /// art: either an authored color was given and the quad isn't full-color, or the dark-canvas
        /// invert applies. A full-color quad (finger/fingers) never inks, whether or not a color was
        /// authored on it - the game refuses to color one rather than flattening it to a silhouette.
        /// </summary>
        /// <param name="quad">The zero-based icon quad.</param>
        /// <param name="dark">Whether the canvas is dark.</param>
        /// <param name="hasAuthoredColor">Whether the prompt authored a <c>color</c> attribute.</param>
        public static bool ShouldInk(int quad, bool dark, bool hasAuthoredColor)
        {
            bool colored = hasAuthoredColor && !IsColoredQuad(quad);
            return colored || ShouldInvert(quad, dark);
        }

        /// <summary>Sets <c>locale="en"</c> when absent (leaves any existing value untouched).</summary>
        public static void EnsureEnglishLocale(LevelObject o)
        {
            if (o.GetAttr("locale") is null)
            {
                o.SetAttr("locale", "en");
            }
        }
    }
}

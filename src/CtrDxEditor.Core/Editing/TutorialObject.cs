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
        /// <summary>Number of tutorial icon quads / tags.</summary>
        public const int IconCount = 11;

        /// <summary>Element placed for a new tutorial icon.</summary>
        public const string DefaultElement = "tutorial01";

        /// <summary>Default wrap width for a new tutorial text.</summary>
        public const int DefaultTextWidth = 140;

        /// <summary>Placeholder text so a freshly placed tutorial text is visible.</summary>
        public const string DefaultText = "Tutorial text";

        /// <summary>The tutorial-text element name.</summary>
        public const string TextElement = "tutorialText";

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
            if (type is not { Length: 10 } || !type.StartsWith("tutorial", System.StringComparison.Ordinal))
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
        public static bool ShouldInvert(int quad, bool dark)
        {
            return dark && !IsColoredQuad(quad);
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

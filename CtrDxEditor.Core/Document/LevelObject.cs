using System.Globalization;
using System.Xml.Linq;

namespace CtrDxEditor.Core.Document
{
    /// <summary>A live wrapper over an Objects-layer element. Edits write back to the XElement.</summary>
    public sealed class LevelObject(XElement element)
    {
        /// <summary>The wrapped XML element for this level object.</summary>
        public XElement Element { get; } = element;

        /// <summary>The object's XML element name.</summary>
        public string Type => Element.Name.LocalName;

        /// <summary>The object's x coordinate in level space.</summary>
        public int X
        {
            get => ReadInt("x");
            set => Element.SetAttributeValue("x", value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>The object's y coordinate in level space.</summary>
        public int Y
        {
            get => ReadInt("y");
            set => Element.SetAttributeValue("y", value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Reads a raw XML attribute value.</summary>
        public string? GetAttr(string name)
        {
            return Element.Attribute(name)?.Value;
        }

        /// <summary>Sets a raw XML attribute value.</summary>
        public void SetAttr(string name, string value)
        {
            Element.SetAttributeValue(name, value);
        }

        // Identity follows the wrapped element, so two wrappers over the same node are equal.
        // Lets a freshly re-read Objects list match a selection captured from a different read.
        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LevelObject other && ReferenceEquals(other.Element, Element);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Element.GetHashCode();
        }

        private int ReadInt(string name)
        {
            return int.TryParse(
                Element.Attribute(name)?.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int v)
                ? v
                : 0;
        }
    }
}

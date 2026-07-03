using System.Globalization;
using System.Xml.Linq;

namespace CtrDxEditor.Core.Document
{
    /// <summary>A live wrapper over an Objects-layer element. Edits write back to the XElement.</summary>
    public sealed class LevelObject(XElement element)
    {
        public XElement Element { get; } = element;

        public string Type => Element.Name.LocalName;

        public int X
        {
            get => ReadInt("x");
            set => Element.SetAttributeValue("x", value.ToString(CultureInfo.InvariantCulture));
        }

        public int Y
        {
            get => ReadInt("y");
            set => Element.SetAttributeValue("y", value.ToString(CultureInfo.InvariantCulture));
        }

        public string? GetAttr(string name)
        {
            return Element.Attribute(name)?.Value;
        }

        public void SetAttr(string name, string value)
        {
            Element.SetAttributeValue(name, value);
        }

        // Identity follows the wrapped element, so two wrappers over the same node are equal.
        // Lets a freshly re-read Objects list match a selection captured from a different read.
        public override bool Equals(object? obj)
        {
            return obj is LevelObject other && ReferenceEquals(other.Element, Element);
        }

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

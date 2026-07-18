using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CtrDxEditor.Core.Document
{
    /// <summary>A live wrapper over one non-settings <c>&lt;layer&gt;</c> element and its objects.</summary>
    public sealed class LevelLayer(XElement element)
    {
        /// <summary>The wrapped <c>&lt;layer&gt;</c> element.</summary>
        public XElement Element { get; } = element;

        /// <summary>The layer's name attribute, or an empty string when absent.</summary>
        public string Name => (string?)Element.Attribute("name") ?? "";

        /// <summary>The layer's direct child objects, in XML order.</summary>
        public IReadOnlyList<LevelObject> Objects =>
            [.. Element.Elements().Select(e => new LevelObject(e))];

        /// <summary>Sets the layer's name attribute.</summary>
        /// <param name="name">The new layer name.</param>
        public void Rename(string name)
        {
            Element.SetAttributeValue("name", name);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CtrDxEditor.Core.Document
{
    /// <summary>
    /// Owns the parsed level XML tree. Unknown layers, elements, and attributes are retained
    /// verbatim on the underlying XDocument so a no-edit save round-trips losslessly.
    /// </summary>
    public sealed class LevelDocument
    {
        private readonly XDocument _doc;

        private LevelDocument(XDocument doc)
        {
            _doc = doc;
        }

        public static LevelDocument Parse(string xml)
        {
            return new(XDocument.Parse(xml));
        }

        public static LevelDocument Load(string path)
        {
            return new(XDocument.Load(path));
        }

        private XElement Root => _doc.Root
            ?? throw new InvalidDataException("Level XML has no root <map> element.");

        private XElement? Layer(string name)
        {
            return Root.Elements("layer")
            .FirstOrDefault(l => (string?)l.Attribute("name") == name);
        }

        private XElement? SettingsMap => Layer("settings")?.Element("map");

        private XElement? GameDesign => Layer("settings")?.Element("gameDesign");

        public int GridSize => ReadInt(SettingsMap, "gridSize", 32);

        public int Width => ReadInt(SettingsMap, "width", 0);

        public int Height => ReadInt(SettingsMap, "height", 0);

        public bool TwoParts =>
            bool.TryParse(GameDesign?.Attribute("twoParts")?.Value, out bool v) && v;

        public XElement? ObjectsLayer => Layer("Objects");

        public void Add(LevelObject obj)
        {
            XElement layer = ObjectsLayer ?? CreateObjectsLayer();
            layer.Add(obj.Element);
        }

        public void Remove(LevelObject obj)
        {
            obj.Element.Remove();
        }

        public IReadOnlyList<LevelObject> Objects =>
            ObjectsLayer is null ? [] : [.. ObjectsLayer.Elements().Select(e => new LevelObject(e))];

        public string Save()
        {
            XDeclaration decl = _doc.Declaration ?? new XDeclaration("1.0", "utf-8", null);
            return decl + Environment.NewLine + Root;
        }

        private static int ReadInt(XElement? el, string attr, int fallback)
        {
            return int.TryParse(
                el?.Attribute(attr)?.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int v)
                ? v
                : fallback;
        }

        private XElement CreateObjectsLayer()
        {
            XElement layer = new("layer", new XAttribute("name", "Objects"));
            Root.Add(layer);
            return layer;
        }
    }
}

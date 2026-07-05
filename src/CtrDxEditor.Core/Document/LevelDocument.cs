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

        /// <summary>Parses a level document from an XML string.</summary>
        public static LevelDocument Parse(string xml)
        {
            return new(XDocument.Parse(xml));
        }

        /// <summary>Loads a level document from disk.</summary>
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

        /// <summary>The level grid size in map units, defaulting to 32 when absent.</summary>
        public int GridSize => ReadInt(SettingsMap, "gridSize", 32);

        /// <summary>The level width in map units.</summary>
        public int Width => ReadInt(SettingsMap, "width", 0);

        /// <summary>The level height in map units.</summary>
        public int Height => ReadInt(SettingsMap, "height", 0);

        /// <summary>Whether the level uses the two-candy split layout.</summary>
        public bool TwoParts =>
            bool.TryParse(GameDesign?.Attribute("twoParts")?.Value, out bool v) && v;

        /// <summary>The rope physics speed multiplier, defaulting to 1.0 when absent.</summary>
        public float RopePhysicsSpeed => ReadFloat(GameDesign, "ropePhysicsSpeed", 1.0f);

        /// <summary>The special tutorial-trigger id, defaulting to 0 when absent.</summary>
        public int Special => ReadInt(GameDesign, "special", 0);

        /// <summary>Whether the level is a night level (uses light bulbs).</summary>
        public bool NightLevel =>
            bool.TryParse(GameDesign?.Attribute("nightLevel")?.Value, out bool v) && v;

        /// <summary>All editable level-wide settings read from the settings layer.</summary>
        public LevelSettings Settings =>
            new(Width, Height, RopePhysicsSpeed, Special, TwoParts, NightLevel);

        /// <summary>The Objects layer element, or null when the document has none yet.</summary>
        public XElement? ObjectsLayer => Layer("Objects");

        /// <summary>Adds an object to the Objects layer, creating the layer when needed.</summary>
        public void Add(LevelObject obj)
        {
            XElement layer = ObjectsLayer ?? CreateObjectsLayer();
            layer.Add(obj.Element);
        }

        /// <summary>Removes an object from its parent XML document.</summary>
        public static void Remove(LevelObject obj)
        {
            obj.Element.Remove();
        }

        /// <summary>Current editable objects in the Objects layer, in XML order.</summary>
        public IReadOnlyList<LevelObject> Objects =>
            ObjectsLayer is null ? [] : [.. ObjectsLayer.Elements().Select(e => new LevelObject(e))];

        /// <summary>Serializes the level XML, preserving the original declaration when present.</summary>
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

        private static float ReadFloat(XElement? el, string attr, float fallback)
        {
            return float.TryParse(
                el?.Attribute(attr)?.Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float v)
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

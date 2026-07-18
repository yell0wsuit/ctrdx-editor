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
    /// verbatim on the underlying XDocument so a no-edit save round-trips losslessly. Load-time
    /// fixups (binding-key and spike/bouncer size normalization) are applied by LevelObjectPolicy
    /// during editor load, not here, so a bare Parse stays lossless.
    /// </summary>
    public sealed class LevelDocument
    {
        private readonly XDocument _doc;

        private LevelDocument(XDocument doc)
        {
            _doc = doc;
        }

        /// <summary>Parses a level document from an XML string.</summary>
        /// <param name="xml">The level XML.</param>
        /// <returns>The parsed document.</returns>
        public static LevelDocument Parse(string xml)
        {
            return new(XDocument.Parse(xml));
        }

        /// <summary>Loads a level document from disk.</summary>
        /// <param name="path">Filesystem path to the level XML.</param>
        /// <returns>The parsed document.</returns>
        public static LevelDocument Load(string path)
        {
            return new(XDocument.Load(path));
        }

        /// <summary>Builds a fresh level document with the given settings and an empty Objects layer.</summary>
        /// <param name="settings">The level-wide settings to write into the settings layer.</param>
        /// <returns>A document with no objects.</returns>
        public static LevelDocument CreateNew(LevelSettings settings)
        {
            XElement gameDesignEl = new("gameDesign",
                new XAttribute("ropePhysicsSpeed", settings.RopePhysicsSpeed.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("special", settings.Special.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("twoParts", settings.TwoParts ? "true" : "false"),
                new XAttribute("nightLevel", settings.NightLevel ? "true" : "false"));
            if (settings.UseMobilePhysics)
            {
                gameDesignEl.SetAttributeValue("useMobilePhysics", "true");
            }
            ApplyWater(gameDesignEl, settings);

            XElement settingsLayer = new("layer",
                new XAttribute("name", "settings"),
                new XElement("map",
                    new XAttribute("gridSize", "32"),
                    new XAttribute("width", settings.Width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("height", settings.Height.ToString(CultureInfo.InvariantCulture))),
                gameDesignEl);
            XElement objectsLayer = new("layer", new XAttribute("name", "Objects"));
            XDocument doc = new(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("map", settingsLayer, objectsLayer));
            return new LevelDocument(doc);
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

        /// <summary>The gameDesign settings element, or null when the document has none.</summary>
        public XElement? GameDesignElement => GameDesign;

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

        /// <summary>Whether the level requests the mobile (WP7) physics model.</summary>
        public bool UseMobilePhysics =>
            bool.TryParse(GameDesign?.Attribute("useMobilePhysics")?.Value, out bool v) && v;

        /// <summary>
        /// Height of the bottom-pinned water band in level units, or 0 when the level has no water.
        /// The game scales this by MapScale and builds the band in GameScene.LoadMetadata.
        /// </summary>
        public float Water => ReadFloat(GameDesign, "water", 0f);

        /// <summary>
        /// Rate at which the water drains, in level units per second, or 0 for a static pool. Positive
        /// values lower the water over time (GameScene.Update); it never rises.
        /// </summary>
        public float WaterSpeed => ReadFloat(GameDesign, "waterSpeed", 0f);

        /// <summary>All editable level-wide settings read from the settings layer.</summary>
        public LevelSettings Settings =>
            new(Width, Height, RopePhysicsSpeed, Special, TwoParts, NightLevel, UseMobilePhysics, Water, WaterSpeed);

        /// <summary>All object layers (every <c>&lt;layer&gt;</c> except <c>settings</c>), in document order.</summary>
        public IReadOnlyList<LevelLayer> Layers =>
            [.. Root.Elements("layer")
                .Where(l => (string?)l.Attribute("name") != "settings")
                .Select(l => new LevelLayer(l))];

        /// <summary>Adds an object to the Objects layer, creating the layer when needed.</summary>
        /// <param name="obj">The object to append.</param>
        public void Add(LevelObject obj)
        {
            XElement layer = Layer("Objects") ?? CreateObjectsLayer();
            layer.Add(obj.Element);
        }

        /// <summary>Writes level-wide settings back into the settings layer and adjusts candy objects when split mode changes.</summary>
        /// <param name="settings">The new level-wide settings. Toggling <c>TwoParts</c> converts candy objects in place: on splits <c>candy</c> into <c>candyL</c> plus a centered <c>candyR</c>, off merges them back.</param>
        public void UpdateSettings(LevelSettings settings)
        {
            bool wasTwoParts = TwoParts;
            XElement settingsLayer = Layer("settings") ?? CreateSettingsLayer();
            XElement map = settingsLayer.Element("map") ?? AddChild(settingsLayer, "map");
            XElement gameDesign = settingsLayer.Element("gameDesign") ?? AddChild(settingsLayer, "gameDesign");

            map.SetAttributeValue("gridSize", "32");
            map.SetAttributeValue("width", settings.Width.ToString(CultureInfo.InvariantCulture));
            map.SetAttributeValue("height", settings.Height.ToString(CultureInfo.InvariantCulture));
            gameDesign.SetAttributeValue("ropePhysicsSpeed", settings.RopePhysicsSpeed.ToString(CultureInfo.InvariantCulture));
            gameDesign.SetAttributeValue("special", settings.Special.ToString(CultureInfo.InvariantCulture));
            gameDesign.SetAttributeValue("twoParts", settings.TwoParts ? "true" : "false");
            gameDesign.SetAttributeValue("nightLevel", settings.NightLevel ? "true" : "false");
            if (settings.UseMobilePhysics)
            {
                gameDesign.SetAttributeValue("useMobilePhysics", "true");
            }
            else
            {
                gameDesign.Attribute("useMobilePhysics")?.Remove();
            }
            ApplyWater(gameDesign, settings);

            if (wasTwoParts != settings.TwoParts)
            {
                ConvertCandyForTwoParts(settings.TwoParts, settings.Width, settings.Height);
            }
        }

        /// <summary>Removes an object from its parent XML document.</summary>
        public static void Remove(LevelObject obj)
        {
            obj.Element.Remove();
        }

        /// <summary>
        /// All editable objects across every object layer, flattened in document order. Read-only aggregate
        /// used for level-wide concerns (cardinality, palette gating); it is not a layer and nothing writes
        /// to it. Writes go through a specific <see cref="LevelLayer"/>.
        /// </summary>
        public IReadOnlyList<LevelObject> AllObjects =>
            [.. Layers.SelectMany(l => l.Objects)];

        /// <summary>
        /// Serializes game level XML, preserving the original declaration and unknown data while omitting
        /// editor-only tutorial auto-width attributes emitted by pre-release editor builds.
        /// </summary>
        public string Save()
        {
            XDeclaration decl = _doc.Declaration ?? new XDeclaration("1.0", "utf-8", null);
            XElement exportRoot = new(Root);
            foreach (XElement tutorialText in exportRoot.DescendantsAndSelf("tutorialText"))
            {
                tutorialText.Attribute("autoWidth")?.Remove();
            }
            return decl + Environment.NewLine + exportRoot;
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

        /// <summary>
        /// Writes the water attributes when set and removes them when zero, matching how the game treats a
        /// missing attribute as no water and keeping water-free levels free of noise attributes.
        /// </summary>
        private static void ApplyWater(XElement gameDesign, LevelSettings settings)
        {
            SetOrRemoveFloat(gameDesign, "water", settings.Water);
            SetOrRemoveFloat(gameDesign, "waterSpeed", settings.WaterSpeed);
        }

        /// <summary>Sets an invariant-formatted float attribute, or removes it when the value is zero.</summary>
        private static void SetOrRemoveFloat(XElement el, string attr, float value)
        {
            if (value == 0f)
            {
                el.Attribute(attr)?.Remove();
                return;
            }

            el.SetAttributeValue(attr, value.ToString(CultureInfo.InvariantCulture));
        }

        private XElement CreateSettingsLayer()
        {
            XElement layer = new("layer", new XAttribute("name", "settings"));
            Root.AddFirst(layer);
            return layer;
        }

        private static XElement AddChild(XElement parent, string name)
        {
            XElement child = new(name);
            parent.Add(child);
            return child;
        }

        private XElement CreateObjectsLayer()
        {
            XElement layer = new("layer", new XAttribute("name", "Objects"));
            Root.Add(layer);
            return layer;
        }

        private void ConvertCandyForTwoParts(bool twoParts, int width, int height)
        {
            XElement objects = Layer("Objects") ?? CreateObjectsLayer();
            if (twoParts)
            {
                XElement? candy = objects.Elements("candy").FirstOrDefault();
                XElement? candyL = objects.Elements("candyL").FirstOrDefault();
                if (candyL is null && candy is not null)
                {
                    candy.Name = "candyL";
                    candyL = candy;
                }

                if (candyL is not null && !objects.Elements("candyR").Any())
                {
                    XElement candyR = new("candyR",
                        new XAttribute("x", (width / 2).ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("y", (height / 2).ToString(CultureInfo.InvariantCulture)));
                    objects.Add(candyR);
                }
            }
            else
            {
                if (objects.Elements("candyL").FirstOrDefault() is XElement candyL)
                {
                    candyL.Name = "candy";
                }

                foreach (XElement candyR in objects.Elements("candyR").ToList())
                {
                    candyR.Remove();
                }
            }
        }
    }
}

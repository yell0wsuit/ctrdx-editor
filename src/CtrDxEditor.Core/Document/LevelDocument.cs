using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
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
                if (settings.UseTimeTravelRocketPhysics)
                {
                    gameDesignEl.SetAttributeValue("useTimeTravelRocketPhysics", "true");
                }
            }
            ApplyWater(gameDesignEl, settings);
            ApplyGravity(gameDesignEl, settings);

            XElement mapEl = new("map",
                new XAttribute("gridSize", "32"),
                new XAttribute("width", settings.Width.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("height", settings.Height.ToString(CultureInfo.InvariantCulture)));
            ApplyLevelName(mapEl, settings);

            XElement settingsLayer = new("layer",
                new XAttribute("name", "settings"),
                mapEl,
                gameDesignEl);
            XElement objectsLayer = new("layer", new XAttribute("name", "Objects"));
            XDocument doc = new(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("map", settingsLayer, objectsLayer));
            return new LevelDocument(doc);
        }

        private XElement Root => _doc.Root
            ?? throw new InvalidDataException("Level XML has no root <map> element.");

        /// <summary>Whether a layer name identifies the special settings layer.</summary>
        /// <param name="name">The layer name to classify.</param>
        /// <returns>True for <c>settings</c> in any casing.</returns>
        public static bool IsSettingsLayerName(string? name)
        {
            return string.Equals(name, "settings", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSettingsLayer(XElement layer)
        {
            return IsSettingsLayerName((string?)layer.Attribute("name"));
        }

        private XElement? SettingsLayer => Root.Elements("layer").FirstOrDefault(IsSettingsLayer);

        /// <summary>The number of settings layers, matched without regard to casing.</summary>
        public int SettingsLayerCount => Root.Elements("layer").Count(IsSettingsLayer);

        private XElement? SettingsMap => SettingsLayer?.Element("map");

        private XElement? GameDesign => SettingsLayer?.Element("gameDesign");

        /// <summary>The gameDesign settings element, or null when the document has none.</summary>
        public XElement? GameDesignElement => GameDesign;

        /// <summary>The level grid size in map units, defaulting to 32 when absent.</summary>
        public int GridSize => ReadInt(SettingsMap, "gridSize", 32);

        /// <summary>The level width in map units.</summary>
        public int Width => ReadInt(SettingsMap, "width", 0);

        /// <summary>The level height in map units.</summary>
        public int Height => ReadInt(SettingsMap, "height", 0);

        /// <summary>
        /// The level's display name, or an empty string when the map carries none. The game resolves it
        /// through its string table (shipped packs store a key) and shows the raw text when no entry matches.
        /// </summary>
        public string LevelName => SettingsMap?.Attribute("levelName")?.Value ?? string.Empty;

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
        /// Whether the level requests Cut the Rope: Time Travel's rocket tuning. The editor offers this
        /// only as a mode of <see cref="UseMobilePhysics"/>, so a file that sets it alone reads back
        /// false here - the attribute itself is still round-tripped untouched.
        /// </summary>
        public bool UseTimeTravelRocketPhysics =>
            UseMobilePhysics
            && bool.TryParse(GameDesign?.Attribute("useTimeTravelRocketPhysics")?.Value, out bool v) && v;

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

        /// <summary>
        /// Horizontal gravity for the level, positive pointing right. Absent means the game's default of none.
        /// </summary>
        public float GravityX => ReadFloat(GameDesign, "globalGravityX", LevelGravity.DefaultX);

        /// <summary>
        /// Vertical gravity for the level, positive pulling downward. Absent means normal Earth gravity, which
        /// is why an explicit 0 (weightless) is a different level from one carrying no attribute.
        /// </summary>
        public float GravityY => ReadFloat(GameDesign, "globalGravityY", LevelGravity.DefaultY);

        /// <summary>All editable level-wide settings read from the settings layer.</summary>
        public LevelSettings Settings =>
            new(Width, Height, RopePhysicsSpeed, Special, TwoParts, NightLevel, UseMobilePhysics,
                UseTimeTravelRocketPhysics, Water, WaterSpeed, LevelName, GravityX, GravityY);

        /// <summary>All object layers (every <c>&lt;layer&gt;</c> except <c>settings</c>), in document order.</summary>
        public IReadOnlyList<LevelLayer> Layers =>
            [.. Root.Elements("layer")
                .Where(l => !IsSettingsLayer(l))
                .Select(l => new LevelLayer(l))];

        /// <summary>Appends a new empty object layer with the given name.</summary>
        /// <param name="name">The new layer's name.</param>
        /// <returns>The created layer wrapper.</returns>
        public LevelLayer AddLayer(string name)
        {
            XElement el = new("layer", new XAttribute("name", name));
            Root.Add(el);
            return new LevelLayer(el);
        }

        /// <summary>Removes an object layer and every object inside it.</summary>
        /// <param name="layer">The layer to remove.</param>
        public void RemoveLayer(LevelLayer layer)
        {
            if (!ReferenceEquals(layer.Element.Parent, Root))
            {
                return;
            }

            layer.Element.Remove();
        }

        /// <summary>Reorders an object layer among the other object layers, clamped to the valid range.</summary>
        /// <param name="layer">The layer to move.</param>
        /// <param name="delta">Positions to shift; negative moves earlier, positive later.</param>
        public void MoveLayer(LevelLayer layer, int delta)
        {
            List<XElement> objectLayers = [.. Root.Elements("layer")
                .Where(l => !IsSettingsLayer(l))];
            int from = objectLayers.IndexOf(layer.Element);
            if (from < 0)
            {
                return;
            }

            int to = Math.Clamp(from + delta, 0, objectLayers.Count - 1);
            if (to == from)
            {
                return;
            }

            layer.Element.Remove();
            if (to == 0)
            {
                objectLayers[0].AddBeforeSelf(layer.Element);
            }
            else
            {
                objectLayers[to <= from ? to - 1 : to].AddAfterSelf(layer.Element);
            }
        }

        /// <summary>Appends an object to a specific layer.</summary>
        /// <param name="obj">The object to add.</param>
        /// <param name="target">The layer to add it to.</param>
        public void Add(LevelObject obj, LevelLayer target)
        {
            if (!ReferenceEquals(target.Element.Parent, Root))
            {
                return;
            }

            target.Element.Add(obj.Element);
        }

        /// <summary>Moves an object out of its current layer and appends it to another.</summary>
        /// <param name="obj">The object to move.</param>
        /// <param name="target">The destination layer.</param>
        public void MoveObject(LevelObject obj, LevelLayer target)
        {
            if (!ReferenceEquals(target.Element.Parent, Root))
            {
                return;
            }

            obj.Element.Remove();
            target.Element.Add(obj.Element);
        }

        /// <summary>Trims and validates a candidate layer name.</summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="normalized">The trimmed name when validation succeeds; otherwise an empty string.</param>
        /// <param name="excluding">A layer to ignore in the uniqueness check (the one being renamed).</param>
        /// <returns>True when the normalized name is XML-safe, non-empty, and unique.</returns>
        public bool TryNormalizeLayerName(string? name, out string normalized, LevelLayer? excluding = null)
        {
            return ValidateLayerName(name, out normalized, excluding) == LayerNameValidationError.None;
        }

        /// <summary>Trims a candidate layer name and reports why it cannot be used.</summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="normalized">The trimmed name when validation succeeds; otherwise an empty string.</param>
        /// <param name="excluding">A layer to ignore in the uniqueness check (the one being renamed).</param>
        /// <returns>The validation result.</returns>
        public LayerNameValidationError ValidateLayerName(
            string? name,
            out string normalized,
            LevelLayer? excluding = null)
        {
            string candidate = name?.Trim() ?? "";
            normalized = "";
            if (candidate.Length == 0)
            {
                return LayerNameValidationError.Empty;
            }

            if (IsSettingsLayerName(candidate))
            {
                return LayerNameValidationError.Reserved;
            }

            try
            {
                _ = XmlConvert.VerifyXmlChars(candidate);
            }
            catch (XmlException)
            {
                return LayerNameValidationError.InvalidXml;
            }

            if (Layers.Any(layer =>
                !ReferenceEquals(layer.Element, excluding?.Element) && layer.Name == candidate))
            {
                return LayerNameValidationError.Duplicate;
            }

            normalized = candidate;
            return LayerNameValidationError.None;
        }

        /// <summary>True when a normalized layer name is valid and not already used by another object layer.</summary>
        /// <param name="name">The candidate name.</param>
        /// <param name="excluding">A layer to ignore in the uniqueness check (the one being renamed).</param>
        /// <returns>Whether the name may be applied.</returns>
        public bool IsLayerNameAvailable(string name, LevelLayer? excluding = null)
        {
            return TryNormalizeLayerName(name, out _, excluding);
        }

        /// <summary>Renames duplicate ordinary layers deterministically in document order.</summary>
        /// <returns>True when at least one layer was renamed.</returns>
        public bool NormalizeDuplicateLayerNames()
        {
            XElement[] ordinaryLayers = [.. Root.Elements("layer").Where(layer => !IsSettingsLayer(layer))];
            HashSet<string> reservedNames = ordinaryLayers
                .Select(layer => (string?)layer.Attribute("name") ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> seenNames = Enumerable.Empty<string>().ToHashSet(StringComparer.Ordinal);
            bool changed = false;

            foreach (XElement layer in ordinaryLayers)
            {
                string originalName = (string?)layer.Attribute("name") ?? string.Empty;
                if (seenNames.Add(originalName))
                {
                    continue;
                }

                int suffix = 2;
                string candidate;
                do
                {
                    candidate = $"{originalName}-{suffix}";
                    suffix++;
                }
                while (reservedNames.Contains(candidate));

                layer.SetAttributeValue("name", candidate);
                _ = reservedNames.Add(candidate);
                _ = seenNames.Add(candidate);
                changed = true;
            }

            return changed;
        }

        /// <summary>Writes level-wide settings back into the settings layer and adjusts candy objects when split mode changes.</summary>
        /// <param name="settings">The new level-wide settings. Toggling <c>TwoParts</c> converts candy objects in place: on splits <c>candy</c> into <c>candyL</c> plus a centered <c>candyR</c>, off merges them back.</param>
        public void UpdateSettings(LevelSettings settings)
        {
            bool wasTwoParts = TwoParts;
            XElement settingsLayer = SettingsLayer ?? CreateSettingsLayer();
            XElement map = settingsLayer.Element("map") ?? AddChild(settingsLayer, "map");
            XElement gameDesign = settingsLayer.Element("gameDesign") ?? AddChild(settingsLayer, "gameDesign");

            map.SetAttributeValue("gridSize", "32");
            map.SetAttributeValue("width", settings.Width.ToString(CultureInfo.InvariantCulture));
            map.SetAttributeValue("height", settings.Height.ToString(CultureInfo.InvariantCulture));
            ApplyLevelName(map, settings);
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

            // A mode of mobile physics, so it is never written on its own; turning mobile physics off
            // takes it with it.
            if (settings.UseMobilePhysics && settings.UseTimeTravelRocketPhysics)
            {
                gameDesign.SetAttributeValue("useTimeTravelRocketPhysics", "true");
            }
            else
            {
                gameDesign.Attribute("useTimeTravelRocketPhysics")?.Remove();
            }
            ApplyWater(gameDesign, settings);
            ApplyGravity(gameDesign, settings);

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

        /// <summary>Resolves a structural coordinate to the live object it points at, or null when out of range.</summary>
        /// <param name="reference">The coordinate to resolve.</param>
        /// <returns>The live object, or null.</returns>
        public LevelObject? Resolve(ObjectRef reference)
        {
            IReadOnlyList<LevelLayer> layers = Layers;
            if (reference.LayerIndex < 0 || reference.LayerIndex >= layers.Count)
            {
                return null;
            }

            IReadOnlyList<LevelObject> objects = layers[reference.LayerIndex].Objects;
            return reference.IndexInLayer >= 0 && reference.IndexInLayer < objects.Count
                ? objects[reference.IndexInLayer]
                : null;
        }

        /// <summary>Finds the structural coordinate of a live object, or null when it is not in any layer.</summary>
        /// <param name="obj">The object to locate.</param>
        /// <returns>Its coordinate, or null.</returns>
        public ObjectRef? RefOf(LevelObject obj)
        {
            IReadOnlyList<LevelLayer> layers = Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                IReadOnlyList<LevelObject> objects = layers[layerIndex].Objects;
                for (int indexInLayer = 0; indexInLayer < objects.Count; indexInLayer++)
                {
                    if (Equals(objects[indexInLayer], obj))
                    {
                        return new ObjectRef(layerIndex, indexInLayer);
                    }
                }
            }

            return null;
        }

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

        /// <summary>
        /// Writes the trimmed level name, or removes the attribute when the name is blank, so a level that
        /// never had one stays free of the attribute.
        /// </summary>
        private static void ApplyLevelName(XElement map, LevelSettings settings)
        {
            string name = settings.LevelName?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                map.Attribute("levelName")?.Remove();
                return;
            }

            map.SetAttributeValue("levelName", name);
        }

        /// <summary>
        /// Writes the gravity attributes when they differ from the game's defaults and removes them otherwise.
        /// Each axis is compared against its own default rather than zero, so a weightless level keeps its
        /// explicit <c>globalGravityY="0"</c> instead of silently reverting to Earth gravity on the next load.
        /// </summary>
        private static void ApplyGravity(XElement gameDesign, LevelSettings settings)
        {
            SetOrRemoveFloat(gameDesign, "globalGravityX", settings.GravityX, LevelGravity.DefaultX);
            SetOrRemoveFloat(gameDesign, "globalGravityY", settings.GravityY, LevelGravity.DefaultY);
        }

        /// <summary>Sets an invariant-formatted float attribute, or removes it when it holds its default.</summary>
        private static void SetOrRemoveFloat(XElement el, string attr, float value, float defaultValue = 0f)
        {
            if (value == defaultValue)
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

        private void ConvertCandyForTwoParts(bool twoParts, int width, int height)
        {
            XElement[] objectLayers = [.. Layers.Select(layer => layer.Element)];
            if (twoParts)
            {
                XElement? candy = objectLayers.SelectMany(layer => layer.Elements("candy")).FirstOrDefault();
                XElement? candyL = objectLayers.SelectMany(layer => layer.Elements("candyL")).FirstOrDefault();
                if (candyL is null && candy is not null)
                {
                    candy.Name = "candyL";
                    candyL = candy;
                }

                bool hasCandyR = objectLayers.SelectMany(layer => layer.Elements("candyR")).Any();
                if (candyL?.Parent is XElement candyLayer && !hasCandyR)
                {
                    XElement candyR = new("candyR",
                        new XAttribute("x", (width / 2).ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("y", (height / 2).ToString(CultureInfo.InvariantCulture)));
                    candyLayer.Add(candyR);
                }
            }
            else
            {
                if (objectLayers.SelectMany(layer => layer.Elements("candyL")).FirstOrDefault() is XElement candyL)
                {
                    candyL.Name = "candy";
                }

                foreach (XElement candyR in objectLayers.SelectMany(layer => layer.Elements("candyR")).ToList())
                {
                    candyR.Remove();
                }
            }
        }
    }
}

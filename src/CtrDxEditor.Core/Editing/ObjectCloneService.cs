using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Duplicates objects by deep-copying their XML, dropping any clone whose type is already at its placement
    /// cap, assigning fresh auto-numbered keys, and remapping grab bindings that point at a co-cloned
    /// candy, bulb, or axe.
    /// </summary>
    public static class ObjectCloneService
    {
        /// <summary>
        /// Clones <paramref name="source"/> into <paramref name="target"/> and returns the surviving clones
        /// (already appended to the layer). Types at capacity are skipped.
        /// </summary>
        public static IReadOnlyList<LevelObject> Clone(
            IReadOnlyList<LevelObject> source, LevelLayer target, LevelDocument doc)
        {
            DescriptorTable table = DescriptorTable.CtrObjects;
            List<LevelObject> clones = [];
            Dictionary<string, string> candyRemap = [];
            Dictionary<string, string> bulbRemap = [];
            Dictionary<string, string> axeRemap = [];

            foreach (LevelObject src in source)
            {
                ObjectDescriptor? descriptor = table.For(src.Type);
                if (descriptor is not null && Cardinality.IsAtCapacity(descriptor, doc.AllObjects))
                {
                    continue;
                }

                LevelObject clone = new(new XElement(src.Element));
                string? oldCandy = src.GetAttr("candyNumber");
                string? oldBulb = src.GetAttr("bulbNumber");
                string? oldAxe = src.GetAttr(AxeBinding.KeyAttribute);
                LevelObjectPolicy.ApplyDefaults(clone, doc);
                doc.Add(clone, target);

                if (clone.Type == "candy" && oldCandy is not null
                    && clone.GetAttr("candyNumber") is { } newCandy)
                {
                    candyRemap[oldCandy.Trim()] = newCandy;
                }
                if (clone.Type is "lightBulb" or "lightbulb" && oldBulb is not null
                    && clone.GetAttr("bulbNumber") is { } newBulb)
                {
                    bulbRemap[oldBulb.Trim()] = newBulb;
                }
                if (AxeBinding.IsAxe(clone) && oldAxe is not null
                    && clone.GetAttr(AxeBinding.KeyAttribute) is { } newAxe)
                {
                    axeRemap[oldAxe.Trim()] = newAxe;
                }

                clones.Add(clone);
            }

            foreach (LevelObject grab in clones.Where(o => o.Type == "grab"))
            {
                if (IsTrue(grab.GetAttr("bindBulb")))
                {
                    Retarget(grab, "bulbNumber", bulbRemap);
                }
                else if (grab.GetAttr(AxeBinding.KeyAttribute) is not null)
                {
                    // A co-cloned axe took a fresh key, so its grab has to follow it rather than stay
                    // pointed at the original.
                    Retarget(grab, AxeBinding.KeyAttribute, axeRemap);
                }
                else
                {
                    Retarget(grab, "candyNumber", candyRemap);
                }
            }

            return clones;
        }

        private static void Retarget(LevelObject grab, string attribute, Dictionary<string, string> map)
        {
            if (grab.GetAttr(attribute) is { } old && map.TryGetValue(old.Trim(), out string? next))
            {
                grab.SetAttr(attribute, next);
            }
        }

        private static bool IsTrue(string? value)
        {
            return bool.TryParse(value, out bool parsed) && parsed;
        }
    }
}

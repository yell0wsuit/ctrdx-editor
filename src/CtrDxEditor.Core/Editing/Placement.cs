using System.Globalization;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Creates new XML-backed level objects from descriptors.</summary>
    public static class Placement
    {
        /// <summary>Segment length seeded into a hand placed on the canvas, long enough to grab and rotate.</summary>
        private const double HandSegmentLength = 50;

        /// <summary>Creates an object at the supplied level coordinates with descriptor defaults applied.</summary>
        /// <param name="descriptor">The object being placed.</param>
        /// <param name="x">Level X coordinate.</param>
        /// <param name="y">Level Y coordinate.</param>
        /// <param name="document">
        /// The level being placed into, when known. Only defaults that depend on the level's settings
        /// consult it; the rest come from the descriptor.
        /// </param>
        public static LevelObject CreateObject(ObjectDescriptor descriptor, int x, int y, LevelDocument? document = null)
        {
            XElement element = new(descriptor.ElementName);
            element.SetAttributeValue("x", x.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("y", y.ToString(CultureInfo.InvariantCulture));
            foreach (AttributeSpec spec in descriptor.Attributes)
            {
                if (spec.Default is not null)
                {
                    element.SetAttributeValue(spec.Name, spec.Default);
                }
            }
            if (descriptor.ElementName == "electro")
            {
                element.SetAttributeValue("size", "5");
            }
            if (descriptor.ElementName == RocketObject.Element)
            {
                // Time Travel levels author impulse in level coordinates and the game scales it up, so
                // the desktop Experiments default would launch far too hard there.
                element.SetAttributeValue("impulse", RocketObject.ImpulseFor(document));
            }

            LevelObject result = new(element);
            if (HandObject.IsHand(descriptor.ElementName))
            {
                int count = HandObject.SegmentCount(result);
                HandObject.SetSegmentCount(result, count);
                // Seed a longer starting arm than the inactive-slot default so a freshly placed hand is easy
                // to see and grab on the canvas.
                for (int i = 1; i <= count; i++)
                {
                    HandObject.SetLength(result, i, HandSegmentLength);
                }
            }

            return result;
        }
    }
}

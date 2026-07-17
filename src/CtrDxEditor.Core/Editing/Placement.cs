using System.Globalization;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Creates new XML-backed level objects from descriptors.</summary>
    public static class Placement
    {
        /// <summary>Creates an object at the supplied level coordinates with descriptor defaults applied.</summary>
        public static LevelObject CreateObject(ObjectDescriptor descriptor, int x, int y)
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

            LevelObject result = new(element);
            if (HandObject.IsHand(descriptor.ElementName))
            {
                HandObject.SetSegmentCount(result, HandObject.SegmentCount(result));
            }

            return result;
        }
    }
}

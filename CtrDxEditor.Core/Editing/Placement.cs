using System.Globalization;
using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    public static class Placement
    {
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

            return new LevelObject(element);
        }
    }
}

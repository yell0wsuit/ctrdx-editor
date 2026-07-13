using System.Globalization;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Conveyor (game element <c>transporter</c>) semantics that map editor controls to the game's XML.
    /// The game (LoadConveyorBelts.cs / ConveyorBelt.cs) treats any belt whose <c>type</c> is not exactly
    /// "manual" as automatic, and shows the plate arrow only for automatic belts.
    /// </summary>
    public static class ConveyorObject
    {
        /// <summary>The XML element name for a conveyor belt.</summary>
        public const string Element = "transporter";

        /// <summary>Default automatic speed used by palette placement.</summary>
        public const string DefaultVelocity = "10";

        /// <summary>Default movement direction used by palette placement.</summary>
        public const string DefaultDirection = "forward";

        /// <summary>Default belt length used by palette placement.</summary>
        public const string DefaultLength = "250";

        /// <summary>Default belt thickness used by palette placement.</summary>
        public const string DefaultWidth = "50";

        /// <summary>Default belt angle used by palette placement.</summary>
        public const string DefaultAngle = "0";

        /// <summary>Creates the exact automatic conveyor shown while dragging from the palette.</summary>
        /// <param name="x">Anchor X coordinate.</param>
        /// <param name="y">Anchor Y coordinate.</param>
        /// <returns>A new XML-backed conveyor with the placement defaults applied.</returns>
        public static LevelObject CreatePreset(double x, double y)
        {
            XElement element = new(Element);
            element.SetAttributeValue("x", x.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("y", y.ToString(CultureInfo.InvariantCulture));
            element.SetAttributeValue("velocity", DefaultVelocity);
            element.SetAttributeValue("direction", DefaultDirection);
            element.SetAttributeValue("length", DefaultLength);
            element.SetAttributeValue("width", DefaultWidth);
            element.SetAttributeValue("angle", DefaultAngle);
            return new LevelObject(element);
        }

        /// <summary>Whether <paramref name="type"/> is the conveyor element.</summary>
        /// <param name="type">An object element name.</param>
        /// <returns>True when <paramref name="type"/> equals <see cref="Element"/>.</returns>
        public static bool IsConveyor(string type)
        {
            return type == Element;
        }

        /// <summary>True when the belt runs automatically (its <c>type</c> is anything other than "manual").</summary>
        /// <param name="belt">The conveyor object.</param>
        /// <returns>True when the belt is automatic.</returns>
        public static bool IsAuto(LevelObject belt)
        {
            return belt.GetAttr("type") != "manual";
        }

        /// <summary>Sets automatic (removes <c>type</c>) or manual (writes <c>type="manual"</c>).</summary>
        /// <param name="belt">The conveyor object to modify.</param>
        /// <param name="auto">True for automatic, false for manual.</param>
        public static void SetAuto(LevelObject belt, bool auto)
        {
            if (auto)
            {
                belt.RemoveAttr("type");
            }
            else
            {
                belt.SetAttr("type", "manual");
            }
        }

        /// <summary>
        /// The plate arrow direction: 0 for a manual belt (no arrow), otherwise the sign of
        /// <c>velocity * (direction == "forward" ? -1 : 1)</c>, matching the game's adjustedVelocity. A
        /// zero/blank result falls back to +1 so an automatic belt always shows an arrow.
        /// </summary>
        /// <param name="belt">The conveyor object.</param>
        /// <returns>0 (manual), -1, or +1.</returns>
        public static int ArrowSign(LevelObject belt)
        {
            if (!IsAuto(belt))
            {
                return 0;
            }

            double velocity = double.TryParse(
                belt.GetAttr("velocity"), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
            double adjusted = velocity * (belt.GetAttr("direction") == "forward" ? -1.0 : 1.0);
            return adjusted < 0 ? -1 : 1;
        }
    }
}

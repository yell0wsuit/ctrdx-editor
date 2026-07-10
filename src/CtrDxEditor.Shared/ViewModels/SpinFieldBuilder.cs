using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds rotateSpeed-backed spin property fields for spinnable objects.</summary>
    internal static class SpinFieldBuilder
    {
        /// <summary>Adds spin opt-in, positive speed, and direction fields when supported by <see cref="SpinTable"/>.</summary>
        /// <param name="fields">Property field collection to append to.</param>
        /// <param name="value">Selected object whose spin attributes are edited.</param>
        /// <param name="onChanged">Callback invoked after a field writes to the document.</param>
        /// <param name="onChanging">Callback invoked before a field writes to the document.</param>
        /// <param name="rebuild">Callback that rebuilds fields after spin disclosure changes.</param>
        /// <param name="canEnable">Whether the spin checkbox should be editable.</param>
        /// <param name="beforeEnable">Optional normalization to run before spin is enabled.</param>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild,
            bool canEnable = true,
            Action? beforeEnable = null)
        {
            if (!SpinTable.IsSpinnable(value.Type))
            {
                return;
            }

            void Structural()
            {
                onChanged();
                rebuild();
            }

            bool spinning = ObjectSpin.IsSpinning(value);
            bool spinClockwise = ObjectSpin.SpinClockwise(value);

            AttributeFieldViewModel spin = new(
                "spin",
                AttrType.Bool,
                () => spinning ? "true" : "false",
                v =>
                {
                    bool enabled = v == "true";
                    if (enabled && !canEnable)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        beforeEnable?.Invoke();
                    }

                    int spinSpeed = ObjectSpin.SpinSpeed(value);
                    if (enabled && spinSpeed <= 0)
                    {
                        spinSpeed = ObjectSpin.DefaultSpeed;
                    }

                    ObjectSpin.SetSpin(
                        value,
                        enabled,
                        spinSpeed,
                        spinClockwise);

                    Structural();
                },
                Structural,
                onChanging)
            {
                IsEnabled = canEnable,
            };
            fields.Add(spin);

            if (spinning)
            {
                fields.Add(new AttributeFieldViewModel(
                    "spinSpeed",
                    AttrType.Whole,
                    () => SpinSpeedValue(value),
                    v =>
                    {
                        int speed = int.TryParse(
                            v,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int parsed)
                                ? parsed
                                : 0;

                        if (speed <= 0)
                        {
                            value.SetAttr("rotateSpeed", v ?? string.Empty);
                            return;
                        }

                        ObjectSpin.SetSpin(
                            value,
                            enabled: true,
                            speed,
                            spinClockwise);
                    },
                    onChanged,
                    onChanging));

                fields.Add(new AttributeFieldViewModel(
                    "spinClockwise",
                    AttrType.Bool,
                    () => spinClockwise ? "true" : "false",
                    v =>
                    {
                        spinClockwise = v == "true";
                        int speed = ObjectSpin.SpinSpeed(value);
                        if (speed > 0)
                        {
                            ObjectSpin.SetSpin(
                                value,
                                enabled: true,
                                speed,
                                clockwise: spinClockwise);
                        }
                    },
                    onChanged,
                    onChanging));
            }

            BuildMovement(fields, value, onChanged, onChanging, rebuild);
        }

        private static void BuildMovement(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            void Structural()
            {
                onChanged();
                rebuild();
            }

            string movementMode = MovementMode(value);
            bool orbitClockwise = ObjectSpin.OrbitClockwise(value);

            fields.Add(new AttributeFieldViewModel(
                "movementMode",
                [
                    new AttributeOptionViewModel("none", "none"),
                    new AttributeOptionViewModel("orbit", "orbit"),
                    new AttributeOptionViewModel("polyline", "polyline"),
                ],
                () => movementMode,
                v =>
                {
                    switch (v)
                    {
                        case "orbit":
                            ObjectSpin.SetOrbital(
                                value,
                                enabled: true,
                                ObjectSpin.OrbitRadius(value),
                                ObjectSpin.OrbitClockwise(value));
                            break;
                        case "polyline":
                            SetPolyline(value);
                            break;
                        default:
                            value.RemoveAttr("path");
                            value.RemoveAttr("moveSpeed");
                            break;
                    }

                    Structural();
                },
                Structural,
                onChanging));

            if (movementMode == "orbit")
            {
                BuildOrbit(fields, value, onChanged, onChanging, orbitClockwise);
            }
            else if (movementMode == "polyline")
            {
                BuildPolyline(fields, value, onChanged, onChanging);
            }
        }

        private static void BuildOrbit(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            bool orbitClockwise)
        {
            fields.Add(new AttributeFieldViewModel(
                "orbitRadius",
                AttrType.Whole,
                () => OrbitRadiusValue(value),
                v =>
                {
                    int radius = int.TryParse(
                        v,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed)
                            ? parsed
                            : 0;

                    if (radius <= 0)
                    {
                        value.SetAttr("path", OrbitPrefix(orbitClockwise) + (v ?? string.Empty));
                        return;
                    }

                    ObjectSpin.SetOrbital(
                        value,
                        enabled: true,
                        radius,
                        orbitClockwise);
                },
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "orbitSpeed",
                AttrType.Whole,
                () => MoveSpeedValue(value),
                v =>
                {
                    int speed = int.TryParse(
                        v,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed)
                            ? parsed
                            : 0;

                    if (speed <= 0)
                    {
                        value.SetAttr("moveSpeed", v ?? string.Empty);
                        return;
                    }

                    ObjectSpin.SetOrbitSpeed(value, speed);
                },
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "orbitClockwise",
                AttrType.Bool,
                () => orbitClockwise ? "true" : "false",
                v =>
                {
                    orbitClockwise = v == "true";
                    string radiusValue = OrbitRadiusValue(value);
                    string speedValue = MoveSpeedValue(value);
                    if (int.TryParse(radiusValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius) && radius > 0)
                    {
                        ObjectSpin.SetOrbital(
                            value,
                            enabled: true,
                            radius,
                            clockwise: orbitClockwise);
                        if (int.TryParse(speedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int speed) && speed > 0)
                        {
                            ObjectSpin.SetOrbitSpeed(value, speed);
                        }
                        else
                        {
                            value.SetAttr("moveSpeed", speedValue);
                        }
                    }
                    else
                    {
                        value.SetAttr("path", OrbitPrefix(orbitClockwise) + radiusValue);
                    }
                },
                onChanged,
                onChanging));
        }

        private static void BuildPolyline(
            IList<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging)
        {
            fields.Add(new AttributeFieldViewModel(
                "polylineSpeed",
                AttrType.Whole,
                () => MoveSpeedValue(value),
                v =>
                {
                    int speed = int.TryParse(
                        v,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int parsed)
                            ? parsed
                            : 0;

                    if (speed <= 0)
                    {
                        value.SetAttr("moveSpeed", v ?? string.Empty);
                        return;
                    }

                    ObjectSpin.SetOrbitSpeed(value, speed);
                },
                onChanged,
                onChanging));

            fields.Add(new AttributeFieldViewModel(
                "polylineLoop",
                AttrType.Bool,
                () => "true",
                v =>
                {
                    if (v != "false")
                    {
                        return;
                    }

                    Vec2 start = new(value.X, value.Y);
                    Vec2[] points = MoverPath.Points(start, value.GetAttr("path"));
                    value.SetAttr("path", MoverPath.SerializePlain(start, points, loop: false));
                },
                onChanged,
                onChanging));
        }

        private static string SpinSpeedValue(LevelObject value)
        {
            string? raw = value.GetAttr("rotateSpeed");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed) && speed != 0
                ? Math.Abs((int)speed).ToString(CultureInfo.InvariantCulture)
                : raw ?? string.Empty;
        }

        private static string OrbitRadiusValue(LevelObject value)
        {
            string? path = value.GetAttr("path");
            return path is { Length: >= 2 } && path[0] == 'R' && (path[1] == 'C' || path[1] == 'W')
                ? path[2..]
                : ObjectSpin.OrbitRadius(value).ToString(CultureInfo.InvariantCulture);
        }

        private static string MoveSpeedValue(LevelObject value)
        {
            string? raw = value.GetAttr("moveSpeed");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed) && speed != 0
                ? Math.Abs((int)speed).ToString(CultureInfo.InvariantCulture)
                : raw ?? string.Empty;
        }

        private static string OrbitPrefix(bool clockwise)
        {
            return clockwise ? "RC" : "RW";
        }

        private static string MovementMode(LevelObject value)
        {
            string? path = value.GetAttr("path");
            return string.IsNullOrWhiteSpace(path) ? "none" : IsOrbitEditingPath(path) ? "orbit" : "polyline";
        }

        private static bool IsOrbitEditingPath(string path)
        {
            return path.Length >= 2 && path[0] == 'R' && (path[1] == 'C' || path[1] == 'W');
        }

        private static void SetPolyline(LevelObject value)
        {
            if (string.IsNullOrWhiteSpace(value.GetAttr("path")) || IsOrbitEditingPath(value.GetAttr("path")!))
            {
                value.SetAttr("path", "100,0");
            }

            int moveSpeed = MoveSpeed(value);
            if (moveSpeed == 0)
            {
                moveSpeed = ObjectSpin.SpinSpeed(value);
            }
            value.SetAttr("moveSpeed", (moveSpeed > 0 ? moveSpeed : ObjectSpin.DefaultSpeed).ToString(CultureInfo.InvariantCulture));
        }

        private static int MoveSpeed(LevelObject value)
        {
            return double.TryParse(value.GetAttr("moveSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double speed)
                ? Math.Abs((int)speed)
                : 0;
        }
    }
}

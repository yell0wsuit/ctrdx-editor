using System;
using System.Collections.ObjectModel;
using System.Globalization;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Builds ant-conveyor fields, including semantic closure and loop direction.</summary>
    public static class AntFieldBuilder
    {
        /// <summary>Adds move speed, semantic closure, and loop direction; path geometry remains canvas-only.</summary>
        /// <param name="fields">Property field collection to append to.</param>
        /// <param name="ants">Selected ant-conveyor object.</param>
        /// <param name="onChanged">Invoked after a field commits a change.</param>
        /// <param name="onChanging">Invoked before a field commits a change for undo capture.</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject ants,
            Action onChanged,
            Action onChanging)
        {
            AttributeFieldViewModel? clockwiseField = null;
            fields.Add(new AttributeFieldViewModel(
                "moveSpeed",
                AttrType.Number,
                () => SpeedMagnitude(ants),
                value =>
                {
                    SetSpeedMagnitude(ants, value);
                    clockwiseField?.Refresh();
                },
                onChanged,
                onChanging)
            {
                NumericMinimumOverride = 1,
            });

            fields.Add(new AttributeFieldViewModel(
                "closedLoop",
                AttrType.Bool,
                () => AntPath.IsClosed(ants.GetAttr("path")) ? "true" : "false",
                value =>
                {
                    AntPath.SetClosed(ants, value == "true");
                    clockwiseField?.Refresh();
                },
                onChanged,
                onChanging,
                () => AntPath.CanSetClosed(ants)));

            clockwiseField = new AttributeFieldViewModel(
                "polylineClockwise",
                AttrType.Bool,
                () => IsMovingClockwise(ants) ? "true" : "false",
                value => SetMovingClockwise(ants, value == "true"),
                onChanged,
                onChanging,
                () => AntPath.CanSetClockwise(ants));
            fields.Add(clockwiseField);
        }

        private static bool IsMovingClockwise(LevelObject ants)
        {
            return AntPath.IsClockwise(ants) != IsNegativeSpeed(ants);
        }

        private static void SetMovingClockwise(LevelObject ants, bool clockwise)
        {
            ants.SetAttr("moveSpeed", SpeedMagnitude(ants));
            AntPath.SetClockwise(ants, clockwise);
        }

        private static string SpeedMagnitude(LevelObject ants)
        {
            string? raw = ants.GetAttr("moveSpeed");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed)
                ? Math.Abs(speed).ToString(CultureInfo.InvariantCulture)
                : raw ?? string.Empty;
        }

        private static void SetSpeedMagnitude(LevelObject ants, string? value)
        {
            if (IsNegativeSpeed(ants) && AntPath.IsClosed(ants.GetAttr("path")))
            {
                AntPath.SetClockwise(ants, !AntPath.IsClockwise(ants));
            }

            string raw = value ?? string.Empty;
            ants.SetAttr(
                "moveSpeed",
                double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed)
                    ? Math.Abs(speed).ToString(CultureInfo.InvariantCulture)
                    : raw);
        }

        private static bool IsNegativeSpeed(LevelObject ants)
        {
            return double.TryParse(
                ants.GetAttr("moveSpeed"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double speed) && speed < 0;
        }
    }
}

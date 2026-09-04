using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;
using CtrDxEditor.Rendering;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds the full tutorial prompt panel: content (icon/text) fields bare, then Trigger, Timing,
    /// Look and Motion as collapsible sections, in that order.
    /// </summary>
    public static class TutorialFieldBuilder
    {
        // Consecutive GroupIndex values distinguish sections that a run of ungrouped (-1) content
        // fields precedes; the numbers only need to differ from each other and from -1.
        private const int TriggerGroupIndex = 0;
        private const int TimingGroupIndex = 1;
        private const int LookGroupIndex = 2;
        private const int MotionGroupIndex = 3;

        private const string DefaultTextHold = "5";
        private const string DefaultSignHold = "5.2";

        private static readonly AttributeOptionViewModel[] IconOptions =
        [
            .. Enumerable.Range(0, TutorialObject.IconCount).Select(quad =>
                new AttributeOptionViewModel(
                    TutorialObject.TagForQuad(quad),
                    Localizer.AttributeOption("icon", TutorialObject.TagForQuad(quad)))),
        ];

        private static readonly AttributeOptionViewModel[] ShowOnOptions =
        [
            .. TutorialEvents.All.Select(e =>
                new AttributeOptionViewModel(TutorialEvents.Name(e), Localizer.AttributeOption("showOn", TutorialEvents.Name(e)))),
        ];

        private static readonly AttributeOptionViewModel[] SubjectOptions =
        [
            new(TutorialSubjects.Name(TutorialSubject.Any), Localizer.AttributeOption("subject", TutorialSubjects.Name(TutorialSubject.Any))),
            new(TutorialSubjects.Name(TutorialSubject.Primary), Localizer.AttributeOption("subject", TutorialSubjects.Name(TutorialSubject.Primary))),
            new(TutorialSubjects.Name(TutorialSubject.Left), Localizer.AttributeOption("subject", TutorialSubjects.Name(TutorialSubject.Left))),
            new(TutorialSubjects.Name(TutorialSubject.Right), Localizer.AttributeOption("subject", TutorialSubjects.Name(TutorialSubject.Right))),
        ];

        private static readonly AttributeOptionViewModel[] MotionModeOptions =
        [
            new("none", Localizer.AttributeOption("motion", "none")),
            new("looping", Localizer.AttributeOption("motion", "looping")),
            new("timed", Localizer.AttributeOption("motion", "timed")),
        ];

        private static readonly AttributeOptionViewModel[] EaseOptions =
        [
            new("none", Localizer.AttributeOption("ease", "none")),
            new("in", Localizer.AttributeOption("ease", "in")),
            new("out", Localizer.AttributeOption("ease", "out")),
        ];

        /// <summary>Appends the fields for a tutorial icon or tutorial text object.</summary>
        /// <param name="fields">The properties-panel field collection to append to.</param>
        /// <param name="value">The tutorial object being edited.</param>
        /// <param name="sprites">Sprite cache, used to measure text for auto-width.</param>
        /// <param name="onChanged">Invoked after a field commits a change.</param>
        /// <param name="onChanging">Invoked before a field commits a change.</param>
        /// <param name="rebuild">Repopulates fields after a structural toggle (icon tag, auto-width, motion mode, path, ...).</param>
        public static void Build(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            SpriteCache sprites,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            bool isText = TutorialObject.IsText(value.Type);

            void Structural()
            {
                onChanged();
                rebuild();
            }

            BuildContent(fields, value, sprites, isText, onChanged, onChanging, Structural);
            BuildTrigger(fields, value, onChanged, onChanging, Structural);
            BuildTiming(fields, value, isText, onChanged, onChanging, Structural);
            BuildLook(fields, value, isText, onChanged, onChanging);
            BuildMotion(fields, value, onChanged, onChanging, Structural);
        }

        private static void BuildContent(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            SpriteCache sprites,
            bool isText,
            Action onChanged,
            Action onChanging,
            Action structural)
        {
            if (isText)
            {
                // Editing the text re-syncs width when auto (so the box grows to fit the text).
                fields.Add(new AttributeFieldViewModel(
                    "text",
                    AttrType.Text,
                    () => value.GetAttr("text"),
                    text =>
                    {
                        value.SetAttr("text", text ?? string.Empty);
                        TutorialRenderer.ApplyAutoWidth(sprites, value);
                    },
                    onChanged,
                    onChanging));

                // Auto-width hides the manual width field and keeps width synced to the text.
                fields.Add(new AttributeFieldViewModel(
                    "autoWidth",
                    AttrType.Bool,
                    () => TutorialObject.IsAutoWidth(value) ? "true" : "false",
                    v =>
                    {
                        TutorialObject.SetAutoWidth(value, v == "true");
                        TutorialRenderer.ApplyAutoWidth(sprites, value);
                    },
                    structural,
                    onChanging));

                fields.Add(new AttributeFieldViewModel(
                    "width",
                    AttrType.Whole,
                    () => value.GetAttr("width"),
                    v => value.SetAttr("width", v ?? string.Empty),
                    onChanged,
                    onChanging,
                    () => !TutorialObject.IsAutoWidth(value)));

                return;
            }

            fields.Add(new AttributeFieldViewModel(
                "icon",
                IconOptions,
                () => value.Type,
                selectedTag =>
                {
                    int quad = TutorialObject.QuadForTag(selectedTag ?? string.Empty);
                    if (quad >= 0)
                    {
                        TutorialObject.SetIcon(value, quad);
                    }
                },
                structural,
                onChanging));
        }

        private static void BuildTrigger(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action structural)
        {
            string header = Localizer.Get("Panel.Trigger");
            _ = TutorialEvents.TryParse(value.GetAttr("showOn"), out TutorialEvent showOn);
            _ = TutorialSubjects.TryParse(value.GetAttr("subject"), out TutorialSubject subject);
            bool hasArea = value.GetAttr("inArea") is not null;
            bool startsCollapsed = showOn == TutorialEvent.Start
                && subject == TutorialSubject.Any
                && !hasArea;

            fields.Add(new AttributeFieldViewModel(
                "showOn",
                ShowOnOptions,
                () => TutorialEvents.TryParse(value.GetAttr("showOn"), out TutorialEvent current)
                    ? TutorialEvents.Name(current)
                    : value.GetAttr("showOn"),
                selected => value.SetAttr("showOn", selected ?? TutorialEvents.Name(TutorialEvent.Start)),
                onChanged,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
                GroupStartsCollapsed = startsCollapsed,
            });

            fields.Add(new AttributeFieldViewModel(
                "subject",
                SubjectOptions,
                () => TutorialSubjects.TryParse(value.GetAttr("subject"), out TutorialSubject current)
                    ? TutorialSubjects.Name(current)
                    : value.GetAttr("subject"),
                selected => value.SetAttr("subject", selected ?? TutorialSubjects.Name(TutorialSubject.Any)),
                onChanged,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
            });

            fields.Add(new AttributeFieldViewModel(
                "inArea",
                AttrType.Bool,
                () => hasArea ? "true" : "false",
                v =>
                {
                    if (v == "true")
                    {
                        value.SetAttr("inArea", new TutorialArea(0, 0, 100, 100).Format());
                    }
                    else
                    {
                        value.RemoveAttr("inArea");
                    }
                },
                structural,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
            });

            AddAreaField(fields, value, "inAreaX", header, a => a.X, (a, v) => a with { X = v }, onChanged, onChanging);
            AddAreaField(fields, value, "inAreaY", header, a => a.Y, (a, v) => a with { Y = v }, onChanged, onChanging);
            AddAreaField(fields, value, "inAreaWidth", header, a => a.Width, (a, v) => a with { Width = v }, onChanged, onChanging);
            AddAreaField(fields, value, "inAreaHeight", header, a => a.Height, (a, v) => a with { Height = v }, onChanged, onChanging);
        }

        private static void AddAreaField(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            string name,
            string header,
            Func<TutorialArea, double> read,
            Func<TutorialArea, double, TutorialArea> write,
            Action onChanged,
            Action onChanging)
        {
            fields.Add(new AttributeFieldViewModel(
                name,
                AttrType.Whole,
                () =>
                {
                    TutorialArea area = RuntimeAreaOrDefault(value.GetAttr("inArea"));
                    return read(area).ToString(CultureInfo.InvariantCulture);
                },
                v =>
                {
                    TutorialArea area = RuntimeAreaOrDefault(value.GetAttr("inArea"));
                    double parsed = int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) ? d : 0;
                    value.SetAttr("inArea", write(area, parsed).Format());
                },
                onChanged,
                onChanging,
                () => value.GetAttr("inArea") is not null)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
            });
        }

        private static TutorialArea RuntimeAreaOrDefault(string? raw)
        {
            if (raw is null)
            {
                return new TutorialArea(0, 0, 100, 100);
            }

            _ = TutorialArea.TryParseRuntime(raw, out TutorialArea area);
            return area;
        }

        private static void BuildTiming(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            bool isText,
            Action onChanged,
            Action onChanging,
            Action structural)
        {
            string header = Localizer.Get("Panel.Timing");
            TutorialTiming timing = TutorialTiming.For(value);
            double defaultHold = isText ? 5.0 : 5.2;
            bool startsCollapsed = string.IsNullOrEmpty(value.GetAttr("group"))
                && timing.Delay == 0.0
                && timing.FadeIn == 1.0
                && timing.Hold == defaultHold
                && timing.FadeOut == 0.5
                && timing.Repeat == 1;

            // A sequencing tag, not a trigger condition - TutorialTrigger.cs never reads it - so it
            // belongs with the rest of the prompt's scheduling, not with Trigger.
            fields.Add(OptionalField(value, "group", AttrType.Text, null, onChanged, onChanging,
                header, TimingGroupIndex, startsCollapsed));

            fields.Add(OptionalField(value, "delay", AttrType.Number, "0", onChanged, onChanging,
                header, TimingGroupIndex));
            fields.Add(OptionalField(value, "fadeIn", AttrType.Number, "1", onChanged, onChanging,
                header, TimingGroupIndex));

            fields.Add(new AttributeFieldViewModel(
                "holdsForever",
                AttrType.Bool,
                () => timing.HoldsForever ? "true" : "false",
                v => value.SetAttr("duration", v == "true" ? "-1" : (isText ? DefaultTextHold : DefaultSignHold)),
                structural,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
            });
            string defaultDuration = isText ? DefaultTextHold : DefaultSignHold;
            fields.Add(OptionalField(value, "duration", AttrType.Number,
                defaultDuration, onChanged, onChanging, header, TimingGroupIndex,
                isEnabled: () => !TutorialTiming.For(value).HoldsForever,
                disabledValue: defaultDuration));

            fields.Add(OptionalField(value, "fadeOut", AttrType.Number, "0.5", onChanged, onChanging,
                header, TimingGroupIndex));

            fields.Add(new AttributeFieldViewModel(
                "repeatsForever",
                AttrType.Bool,
                () => timing.RepeatsForever ? "true" : "false",
                v => value.SetAttr("repeat", v == "true" ? "-1" : "1"),
                structural,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
            });
            // Structural: TutorialMotion.ModeOf treats any authored repeat - including a finite
            // pass count typed here - as a Timed marker on a path-bearing prompt. The field remains
            // visible while Repeat forever owns the -1 sentinel, but is disabled and displays one.
            fields.Add(OptionalField(value, "repeat", AttrType.Whole, "1", structural, onChanging,
                header, TimingGroupIndex,
                isEnabled: () => !TutorialTiming.For(value).RepeatsForever,
                disabledValue: "1"));
        }

        private static void BuildLook(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            bool isText,
            Action onChanged,
            Action onChanging)
        {
            string header = Localizer.Get("Panel.Look");
            TutorialLook look = TutorialLook.For(value);
            bool startsCollapsed = look.Opacity == 1.0
                && look.Color is null
                && look.Angle == 0.0
                && (!isText || (look.Size == 1.0 && look.LineHeight == 1.0));

            fields.Add(OptionalField(value, "opacity", AttrType.Number, "1", onChanged, onChanging,
                header, LookGroupIndex, startsCollapsed));

            // Reads the raw attribute verbatim for display (TutorialColor.Format is not a verbatim
            // reproducer - see its doc comment) and only writes through FormatHex when the user
            // actually edits the value, so an untouched color's authored spelling is never rewritten.
            fields.Add(new AttributeFieldViewModel(
                "color",
                AttrType.Color,
                () => value.GetAttr("color"),
                v =>
                {
                    if (v is not null && TutorialColor.TryParse(v, out TutorialColor color))
                    {
                        value.SetAttr("color", TutorialColor.FormatHex(color.Red, color.Green, color.Blue));
                    }
                    else if (string.IsNullOrWhiteSpace(v))
                    {
                        value.RemoveAttr("color");
                    }
                    else
                    {
                        value.SetAttr("color", v);
                    }
                },
                onChanged,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = LookGroupIndex,
                CanApplyCustomColor = isText || !TutorialObject.IsColoredQuad(TutorialObject.Icon(value)),
            });

            fields.Add(OptionalField(value, "angle", AttrType.Number, "0", onChanged, onChanging,
                header, LookGroupIndex));

            if (isText)
            {
                fields.Add(OptionalField(value, "size", AttrType.Number, "1", onChanged, onChanging,
                    header, LookGroupIndex));
                fields.Add(OptionalField(value, "lineHeight", AttrType.Number, "1", onChanged, onChanging,
                    header, LookGroupIndex));
            }
        }

        private static void BuildMotion(
            ObservableCollection<AttributeFieldViewModel> fields,
            LevelObject value,
            Action onChanged,
            Action onChanging,
            Action structural)
        {
            string header = Localizer.Get("Panel.Motion");
            TutorialMotionMode mode = TutorialMotion.ModeOf(value);

            fields.Add(new AttributeFieldViewModel(
                "motion",
                MotionModeOptions,
                () => ModeToken(mode),
                selected => TutorialMotionEditor.SetMode(value, TokenToMode(selected)),
                structural,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = MotionGroupIndex,
                GroupStartsCollapsed = mode == TutorialMotionMode.None,
            });

            if (mode == TutorialMotionMode.None)
            {
                return;
            }

            // Editing the path can change the leg count (Timed) or drop the mode back to None (an
            // emptied path), either of which changes which fields exist below.
            fields.Add(new AttributeFieldViewModel(value, "path", AttrType.Text, null, structural, onChanging)
            {
                GroupHeader = header,
                GroupIndex = MotionGroupIndex,
                // Attr.path.Help describes the canvas-drag editing hooks and grabs get; a tutorial
                // prompt's path has no such handles, so that help would mislead here.
                HelpText = null,
            });

            fields.Add(OptionalField(
                value,
                "moveSpeed",
                AttrType.Number,
                mode == TutorialMotionMode.Timed ? "100" : "0",
                onChanged,
                onChanging,
                header,
                MotionGroupIndex));

            if (mode == TutorialMotionMode.Looping)
            {
                // Only the shared mover reads rotateSpeed (CTRMover.FromXml); Timed motion clears
                // it because the timeline can't express rotation, so it has no effect there.
                fields.Add(OptionalField(value, "rotateSpeed", AttrType.Number, "0", onChanged, onChanging,
                    header, MotionGroupIndex));
            }

            if (mode != TutorialMotionMode.Timed)
            {
                return;
            }

            fields.Add(OptionalField(value, "moveDelay", AttrType.Number, "0", onChanged, onChanging,
                header, MotionGroupIndex));

            TutorialMotion? motion = TutorialMotion.Timed(value);
            if (motion is null)
            {
                return;
            }

            int legs = motion.Eases.Count;
            for (int leg = 0; leg < legs; leg++)
            {
                int legIndex = leg;
                fields.Add(new AttributeFieldViewModel(
                    "ease",
                    EaseOptions,
                    () => ToToken(TutorialMotion.Timed(value)?.Eases[legIndex] ?? TutorialEase.None),
                    token =>
                    {
                        TutorialMotion? current = TutorialMotion.Timed(value);
                        if (current is null)
                        {
                            return;
                        }

                        TutorialEase[] eases = [.. current.Eases];
                        eases[legIndex] = ToEase(token);
                        value.SetAttr("ease", SerializeEases(eases));
                    },
                    onChanged,
                    onChanging)
                {
                    GroupHeader = header,
                    GroupIndex = MotionGroupIndex,
                });
            }
        }

        private static string ModeToken(TutorialMotionMode mode) => mode switch
        {
            TutorialMotionMode.Looping => "looping",
            TutorialMotionMode.Timed => "timed",
            _ => "none",
        };

        private static TutorialMotionMode TokenToMode(string? token) => token switch
        {
            "looping" => TutorialMotionMode.Looping,
            "timed" => TutorialMotionMode.Timed,
            _ => TutorialMotionMode.None,
        };

        private static string ToToken(TutorialEase ease) => ease switch
        {
            TutorialEase.In => "in",
            TutorialEase.Out => "out",
            _ => "none",
        };

        private static TutorialEase ToEase(string? token) => token switch
        {
            "in" => TutorialEase.In,
            "out" => TutorialEase.Out,
            _ => TutorialEase.None,
        };

        /// <summary>Joins per-leg eases as the single-value shorthand when every leg agrees, else a comma list.</summary>
        private static string SerializeEases(IReadOnlyList<TutorialEase> eases)
        {
            bool allSame = eases.All(e => e == eases[0]);
            return allSame ? ToToken(eases[0]) : string.Join(",", eases.Select(ToToken));
        }

        /// <summary>
        /// Builds an optional XML field that displays the game's effective default while leaving the
        /// attribute absent until it is changed. Clearing the control removes the attribute again.
        /// </summary>
        private static AttributeFieldViewModel OptionalField(
            LevelObject value,
            string name,
            AttrType type,
            string? defaultValue,
            Action onChanged,
            Action onChanging,
            string? groupHeader = null,
            int groupIndex = -1,
            bool groupStartsCollapsed = false,
            Func<bool>? isEnabled = null,
            string? disabledValue = null)
        {
            return new AttributeFieldViewModel(
                name,
                type,
                () => isEnabled?.Invoke() == false
                    ? disabledValue ?? defaultValue
                    : value.GetAttr(name) ?? defaultValue,
                edited =>
                {
                    if (string.IsNullOrWhiteSpace(edited))
                    {
                        value.RemoveAttr(name);
                    }
                    else
                    {
                        value.SetAttr(name, edited);
                    }
                },
                onChanged,
                onChanging,
                isEnabled)
            {
                GroupHeader = groupHeader,
                GroupIndex = groupIndex,
                GroupStartsCollapsed = groupStartsCollapsed,
            };
        }
    }
}

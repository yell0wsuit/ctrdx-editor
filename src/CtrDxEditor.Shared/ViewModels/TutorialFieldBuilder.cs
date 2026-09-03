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

                if (!TutorialObject.IsAutoWidth(value))
                {
                    fields.Add(new AttributeFieldViewModel(value, "width", AttrType.Whole, null, onChanged, onChanging));
                }

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
                && !hasArea
                && string.IsNullOrEmpty(value.GetAttr("group"));

            fields.Add(new AttributeFieldViewModel(
                "showOn",
                ShowOnOptions,
                () => TutorialEvents.Name(showOn),
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
                () => TutorialSubjects.Name(subject),
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

            if (hasArea)
            {
                AddAreaField(fields, value, "inAreaX", header, a => a.X, (a, v) => a with { X = v }, onChanged, onChanging);
                AddAreaField(fields, value, "inAreaY", header, a => a.Y, (a, v) => a with { Y = v }, onChanged, onChanging);
                AddAreaField(fields, value, "inAreaWidth", header, a => a.Width, (a, v) => a with { Width = v }, onChanged, onChanging);
                AddAreaField(fields, value, "inAreaHeight", header, a => a.Height, (a, v) => a with { Height = v }, onChanged, onChanging);
            }

            fields.Add(new AttributeFieldViewModel(value, "group", AttrType.Text, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
            });
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
                AttrType.Number,
                () =>
                {
                    _ = TutorialArea.TryParse(value.GetAttr("inArea"), out TutorialArea area);
                    return read(area).ToString(CultureInfo.InvariantCulture);
                },
                v =>
                {
                    _ = TutorialArea.TryParse(value.GetAttr("inArea"), out TutorialArea area);
                    double parsed = double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0;
                    value.SetAttr("inArea", write(area, parsed).Format());
                },
                onChanged,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TriggerGroupIndex,
            });
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
            bool startsCollapsed = timing.Delay == 0.0
                && timing.FadeIn == 1.0
                && timing.Hold == defaultHold
                && timing.FadeOut == 0.5
                && timing.Repeat == 1;

            fields.Add(new AttributeFieldViewModel(value, "delay", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
                GroupStartsCollapsed = startsCollapsed,
            });
            fields.Add(new AttributeFieldViewModel(value, "fadeIn", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
            });

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
            if (!timing.HoldsForever)
            {
                fields.Add(new AttributeFieldViewModel(value, "duration", AttrType.Number, null, onChanged, onChanging)
                {
                    GroupHeader = header,
                    GroupIndex = TimingGroupIndex,
                });
            }

            fields.Add(new AttributeFieldViewModel(value, "fadeOut", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
            });

            fields.Add(new AttributeFieldViewModel(
                "repeatsForever",
                AttrType.Bool,
                () => timing.RepeatsForever ? "true" : "false",
                v => value.SetAttr("repeat", v == "true" ? "-1" : "2"),
                structural,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = TimingGroupIndex,
            });
            if (!timing.RepeatsForever)
            {
                fields.Add(new AttributeFieldViewModel(value, "repeat", AttrType.Whole, null, onChanged, onChanging)
                {
                    GroupHeader = header,
                    GroupIndex = TimingGroupIndex,
                });
            }
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

            fields.Add(new AttributeFieldViewModel(value, "opacity", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = LookGroupIndex,
                GroupStartsCollapsed = startsCollapsed,
            });

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
                    else
                    {
                        value.SetAttr("color", v ?? string.Empty);
                    }
                },
                onChanged,
                onChanging)
            {
                GroupHeader = header,
                GroupIndex = LookGroupIndex,
            });

            fields.Add(new AttributeFieldViewModel(value, "angle", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = LookGroupIndex,
            });

            if (isText)
            {
                fields.Add(new AttributeFieldViewModel(value, "size", AttrType.Number, null, onChanged, onChanging)
                {
                    GroupHeader = header,
                    GroupIndex = LookGroupIndex,
                });
                fields.Add(new AttributeFieldViewModel(value, "lineHeight", AttrType.Number, null, onChanged, onChanging)
                {
                    GroupHeader = header,
                    GroupIndex = LookGroupIndex,
                });
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

            fields.Add(new AttributeFieldViewModel(value, "moveSpeed", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = MotionGroupIndex,
            });

            if (mode != TutorialMotionMode.Timed)
            {
                return;
            }

            fields.Add(new AttributeFieldViewModel(value, "moveDelay", AttrType.Number, null, onChanged, onChanging)
            {
                GroupHeader = header,
                GroupIndex = MotionGroupIndex,
            });

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
    }
}

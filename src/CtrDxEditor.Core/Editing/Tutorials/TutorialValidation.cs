using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Validates tutorial prompts against the schema enforced by the game's loader.</summary>
    public static class TutorialValidation
    {
        private static readonly string[] TimeAttributes = ["delay", "fadeIn", "duration", "fadeOut", "moveDelay"];
        private static readonly string[] MultiplierAttributes = ["size", "lineHeight", "moveSpeed"];

        /// <summary>Reports every tutorial prompt the game would drop, across all locale copies.</summary>
        public static IEnumerable<LevelWarning> Validate(LevelDocument document)
        {
            List<LevelWarning> findings = [];
            if (document.GameDesignElement?.Attribute("special") is not null)
            {
                findings.Add(Warning("DeadSpecial", "-", "gameDesign"));
            }

            foreach (LevelObject prompt in document.AllObjects)
            {
                if (!TutorialObject.IsText(prompt.Type) && !TutorialObject.IsImage(prompt.Type))
                {
                    continue;
                }

                Check(findings, prompt, document.TwoParts);
            }

            return findings;
        }

        private static void Check(List<LevelWarning> findings, LevelObject prompt, bool twoParts)
        {
            string locale = prompt.GetAttr("locale") ?? "en";
            string element = prompt.Type;
            void AddError(string rule)
            {
                findings.Add(Error(rule, locale, element));
            }

            string? showOn = prompt.GetAttr("showOn");
            if (showOn is not null && !TutorialEvents.TryParse(showOn, out _))
            {
                AddError("UnknownEvent");
            }

            string? subjectValue = prompt.GetAttr("subject");
            bool subjectValid = TutorialSubjects.TryParse(subjectValue, out TutorialSubject subject);
            if (subjectValue is not null && !subjectValid)
            {
                AddError("UnknownSubject");
            }

            TutorialMotionMode motionMode = TutorialMotion.ModeOf(prompt);
            bool pathValid = TryTimedPath(prompt.GetAttr("path"), motionMode, out int legs);
            if (motionMode == TutorialMotionMode.Timed
                && pathValid
                && !TryEases(prompt.GetAttr("ease"), legs))
            {
                AddError("UnknownEase");
            }

            string? areaValue = prompt.GetAttr("inArea");
            if (showOn == "candyMoved" && areaValue is null)
            {
                AddError("AreaRequired");
            }

            if (areaValue is not null && !TutorialArea.TryParse(areaValue, out _))
            {
                AddError("InvalidArea");
            }

            if (subjectValid && subject is TutorialSubject.Left or TutorialSubject.Right && !twoParts)
            {
                AddError("SplitSubject");
            }

            if (TutorialObject.IsImage(prompt.Type)
                && (prompt.GetAttr("size") is not null || prompt.GetAttr("lineHeight") is not null))
            {
                AddError("TextOnlyAttribute");
            }

            if (prompt.GetAttr("anim") is not null)
            {
                AddError("StaleAnimation");
            }

            bool holdForever = TryFinite(prompt.GetAttr("duration"), out double hold) && hold == TutorialTiming.ForeverHold;
            bool repeatValid = TryRepeat(prompt.GetAttr("repeat"), out int repeat);
            if (holdForever && prompt.GetAttr("repeat") is not null && repeatValid && repeat != 1)
            {
                AddError("RepeatWithForeverHold");
            }

            if (prompt.GetAttr("repeat") is not null && !repeatValid)
            {
                AddError("InvalidRepeat");
            }

            if (HasInvalidTime(prompt))
            {
                AddError("InvalidTime");
            }

            string? opacity = prompt.GetAttr("opacity");
            if (opacity is not null
                && (!TryFinite(opacity, out double opacityValue) || opacityValue is < 0 or > 1))
            {
                AddError("InvalidOpacity");
            }

            string? angle = prompt.GetAttr("angle");
            if (angle is not null && !TryFinite(angle, out _))
            {
                AddError("InvalidAngle");
            }

            if (HasInvalidMultiplier(prompt))
            {
                AddError("InvalidMultiplier");
            }

            string? color = prompt.GetAttr("color");
            bool colorValid = color is null || TutorialColor.TryParse(color, out _);
            if (!colorValid)
            {
                AddError("InvalidColor");
            }

            if (colorValid && color is not null && TutorialObject.IsColoredQuad(TutorialObject.Icon(prompt)))
            {
                AddError("ColoredQuad");
            }

            if (!pathValid)
            {
                AddError("InvalidPath");
            }

            TutorialMotion? motion = pathValid && TryEases(prompt.GetAttr("ease"), legs)
                ? TutorialMotion.Timed(prompt)
                : null;
            if (motion is not null && motion.TravelSeconds > TutorialTiming.For(prompt).PassSeconds)
            {
                AddError("TravelExceedsPass");
            }

            if (prompt.GetAttr("special") is not null)
            {
                findings.Add(Warning("DeadSpecial", locale, element));
            }
        }

        private static bool HasInvalidTime(LevelObject prompt)
        {
            foreach (string attribute in TimeAttributes)
            {
                string? value = prompt.GetAttr(attribute);
                if (value is null)
                {
                    continue;
                }

                if (!TryFinite(value, out double parsed))
                {
                    return true;
                }

                if (parsed < 0
                    && (attribute is not "duration" || parsed is not TutorialTiming.ForeverHold))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasInvalidMultiplier(LevelObject prompt)
        {
            foreach (string attribute in MultiplierAttributes)
            {
                string? value = prompt.GetAttr(attribute);
                if (value is not null && (!TryFinite(value, out double parsed) || parsed <= 0))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryTimedPath(string? path, TutorialMotionMode mode, out int legs)
        {
            legs = 0;
            if (mode != TutorialMotionMode.Timed)
            {
                return true;
            }

            if (path is null || MoverPath.IsCircularPath(path))
            {
                return false;
            }

            string trimmed = path.EndsWith(',') ? path[..^1] : path;
            string[] parts = trimmed.Split(',');
            if (parts.Length == 0 || parts.Length % 2 != 0)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length != 0 && !TryFinite(part, out _))
                {
                    return false;
                }
            }

            legs = parts.Length / 2;
            return legs > 0;
        }

        private static bool TryEases(string? value, int legs)
        {
            if (value is null)
            {
                return true;
            }

            string[] parts = value.Split(',');
            foreach (string part in parts)
            {
                if (part is not ("none" or "in" or "out"))
                {
                    return false;
                }
            }

            return parts.Length == 1 || parts.Length == legs;
        }

        private static bool TryRepeat(string? value, out int repeat)
        {
            if (value is null)
            {
                repeat = 1;
                return true;
            }

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out repeat)
                && (repeat > 0 || repeat == TutorialTiming.ForeverRepeat);
        }

        private static bool TryFinite(string? value, out double parsed)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float gameValue)
                && float.IsFinite(gameValue))
            {
                parsed = gameValue;
                return true;
            }

            parsed = 0;
            return false;
        }

        private static LevelWarning Error(string rule, string locale, string element)
        {
            return new LevelWarning($"Validation.Tutorial.{rule}", locale, element)
            {
                Severity = LevelWarningSeverity.Error,
            };
        }

        private static LevelWarning Warning(string rule, string locale, string element)
        {
            return new LevelWarning($"Validation.Tutorial.{rule}", locale, element);
        }
    }
}

using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Composes a tutorial prompt's on-canvas trigger badge text. Kept apart from
    /// <see cref="LevelCanvas"/>, the way <see cref="BadgeRenderer"/> is, so this pure string logic -
    /// join order, the early return, the Invalid short-circuit - is unit-testable without a render
    /// target; the solution defines no <c>InternalsVisibleTo</c>, so public is what makes that possible
    /// (see <see cref="BadgeRenderer"/>'s remarks for the same reasoning).
    /// </summary>
    public static class TutorialBadgeText
    {
        /// <summary>
        /// The badge's full display text for <paramref name="obj"/>, or null when it needs none.
        /// </summary>
        /// <remarks>
        /// Builds the localized Edge/State/Delay/Group phrase <see cref="TutorialBadge.KeyFor"/> picks for
        /// the prompt's primary reason to show a badge, plus a delay and/or group clause whenever those
        /// are authored alongside a real trigger event - KeyFor only ever reports one key, since a prompt
        /// needs at most one primary reason to show a badge at all; the rest are appended here as
        /// suffixes. An unparseable <c>showOn</c> (<see cref="TutorialBadge.InvalidKey"/>) short-circuits
        /// to its own message instead: the game drops that whole prompt, so no delay, group or attempted
        /// event name belongs alongside it.
        /// </remarks>
        public static string? For(LevelObject obj)
        {
            string? key = TutorialBadge.KeyFor(obj);
            if (key is null)
            {
                return null;
            }

            if (key == TutorialBadge.InvalidKey)
            {
                return Localizer.Get(TutorialBadge.InvalidKey);
            }

            List<string> parts = [];
            _ = TutorialEvents.TryParse(obj.GetAttr("showOn"), out TutorialEvent showOn);
            if (showOn != TutorialEvent.Start)
            {
                string eventKey = TutorialEvents.Kind(showOn) == TutorialEventKind.State
                    ? TutorialBadge.StateKey
                    : TutorialBadge.EdgeKey;
                parts.Add(Localizer.Format(eventKey, TriggerLabel(showOn)));
            }

            double delay = TutorialTiming.For(obj).Delay;
            if (delay > 0)
            {
                parts.Add(Localizer.Format(TutorialBadge.DelayKey, delay.ToString("0.###", CultureInfo.InvariantCulture)));
            }

            string? group = obj.GetAttr("group");
            if (!string.IsNullOrEmpty(group))
            {
                parts.Add(Localizer.Format(TutorialBadge.GroupKey, group));
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// The short, badge-scale label for an event - compact where the Trigger dropdown's full sentence
        /// (e.g. "Rope is cut") is not, since this must fit a small canvas chip next to the prompt art.
        /// </summary>
        private static string TriggerLabel(TutorialEvent value)
        {
            return Localizer.Get($"Canvas.Tutorial.Trigger.{TutorialEvents.Name(value)}");
        }
    }
}

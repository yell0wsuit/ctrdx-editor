using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// The tutorial prompt trigger badge: a passive canvas annotation, drawn for every placed prompt (not
    /// only the selection), in the same plate/font/offset chrome as <see cref="DrawGhostBadge"/>. Preview
    /// fires every prompt at t=0 regardless of what would really trigger it - the editor has no simulation
    /// - so without this the canvas would silently misrepresent every triggered prompt in the level.
    /// </summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>Draws a tutorial prompt's trigger badge above its bounds, or nothing when it needs none.</summary>
        private void DrawTutorialBadge(DrawingContext context, ViewTransform v, SpriteCache sprites, LevelObject obj)
        {
            if (!TutorialObject.IsText(obj.Type) && !TutorialObject.IsImage(obj.Type))
            {
                return;
            }

            string? text = TutorialBadgeText(obj);
            if (text is null)
            {
                return;
            }

            LevelBounds bounds = TutorialObject.IsText(obj.Type)
                ? TutorialRenderer.TextBounds(sprites, obj)
                : TutorialRenderer.IconBounds(sprites, obj);
            Vec2 topLeft = v.LevelToScreen(new Vec2(bounds.X, bounds.Y));
            Vec2 topRight = v.LevelToScreen(new Vec2(bounds.X + bounds.W, bounds.Y));
            Point anchor = new((topLeft.X + topRight.X) / 2, topLeft.Y);

            BadgeRenderer.DrawValue(context, text, anchor, Bounds.Size);
        }

        /// <summary>
        /// Builds the badge's full display text: the localized Edge/State/Delay/Group phrase
        /// <see cref="TutorialBadge.KeyFor"/> picks for the prompt's primary reason to show a badge, plus a
        /// delay and/or group clause whenever those are authored alongside a real trigger event (KeyFor
        /// only ever reports one key, since a prompt needs at most one primary reason to show a badge at
        /// all - the rest are appended here as suffixes).
        /// </summary>
        private static string? TutorialBadgeText(LevelObject obj)
        {
            if (TutorialBadge.KeyFor(obj) is null)
            {
                return null;
            }

            List<string> parts = [];
            _ = TutorialEvents.TryParse(obj.GetAttr("showOn"), out TutorialEvent showOn);
            if (showOn != TutorialEvent.Start)
            {
                string key = TutorialEvents.Kind(showOn) == TutorialEventKind.State
                    ? TutorialBadge.StateKey
                    : TutorialBadge.EdgeKey;
                parts.Add(Localizer.Format(key, TutorialTriggerLabel(showOn)));
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
        private static string TutorialTriggerLabel(TutorialEvent value)
        {
            return Localizer.Get($"Canvas.Tutorial.Trigger.{TutorialEvents.Name(value)}");
        }
    }
}

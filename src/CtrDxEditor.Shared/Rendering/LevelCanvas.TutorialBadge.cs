using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws the tutorial prompt trigger badge: a passive canvas annotation, drawn for every placed prompt
    /// (not only the selection), in the same plate/font/offset chrome as <see cref="DrawGhostBadge"/>.
    /// Preview fires every prompt at t=0 regardless of what would really trigger it - the editor has no
    /// simulation - so without this the canvas would silently misrepresent every triggered prompt in the
    /// level. Text composition lives in <see cref="TutorialBadgeText"/>, which is unit-testable on its
    /// own; this partial only positions and draws the result.
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

            string? text = TutorialBadgeText.For(obj);
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
    }
}

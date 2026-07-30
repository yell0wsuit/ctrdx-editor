using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// The selected ghost's morph selector: an interactive badge whose cells switch the previewed state.
    /// Unlike the drag readout it is a control, not a readout — it records hit targets in
    /// <c>_ghostIconHits</c> — so it stays a partial with access to canvas state rather than moving to a
    /// stateless renderer.
    /// </summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>Draws the selected ghost's enabled-state selector badge and records its hit targets.</summary>
        private void DrawGhostBadge(DrawingContext context, ViewTransform v, LevelObject ghost, Point[] outline)
        {
            // A drag owns the slot above the selection, and the selector is not clickable mid-drag anyway.
            // Returning before any hit target is recorded also means a stray press cannot land on a cell
            // that is no longer drawn.
            if (AnyDragActive)
            {
                return;
            }

            IReadOnlyList<GhostMorph> states = GhostStates.Enabled(ghost);
            if (states.Count == 0)
            {
                return;
            }

            const double height = BadgeRenderer.Height;
            const double cellPadding = 14;
            const double separatorGap = 10;

            // Measure each state's localized label (reusing the object names) so the badge fits its
            // text in any language instead of assuming fixed-width abbreviations.
            FormattedText[] labels = new FormattedText[states.Count];
            double[] cellWidths = new double[states.Count];
            double contentWidth = 0;
            for (int i = 0; i < states.Count; i++)
            {
                labels[i] = BadgeRenderer.Label(GhostMorphLabel(states[i]), Brushes.White);
                cellWidths[i] = labels[i].Width + cellPadding;
                contentWidth += cellWidths[i];
            }

            double separatorsWidth = Math.Max(0, states.Count - 1) * separatorGap;
            double width = contentWidth + separatorsWidth + 12;
            double centerX = v.LevelToScreen(new Vec2(ghost.X, ghost.Y)).X;
            Point anchor = new(centerX, outline.Min(p => p.Y));
            Rect bubble = BadgeRenderer.Place(anchor, Bounds.Size, width, height);
            context.FillRectangle(BadgeRenderer.Plate, bubble, BadgeRenderer.Radius);

            double x = bubble.X + 6;
            for (int i = 0; i < states.Count; i++)
            {
                GhostMorph morph = states[i];
                Rect hit = new(x, bubble.Y + 3, cellWidths[i], height - 6);
                if (_ghostPreview.Active == morph)
                {
                    context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 46, 139, 255)), hit, 4);
                }

                context.DrawText(labels[i], new Point(hit.Center.X - (labels[i].Width / 2), hit.Center.Y - (labels[i].Height / 2)));
                _ghostIconHits.Add((hit, morph));
                x += cellWidths[i];

                if (i < states.Count - 1)
                {
                    // Kept as its own regular-weight FormattedText rather than BadgeRenderer.Label, which
                    // is semi-bold. This is a verbatim move; the separator's look must not change.
                    FormattedText separator = new(
                        "|",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(FontFamily.DefaultFontFamilyName),
                        12,
                        Brushes.LightGray);
                    context.DrawText(separator, new Point(x + 2, bubble.Y + ((height - separator.Height) / 2)));
                    x += separatorGap;
                }
            }
        }

        /// <summary>The localized selector label for a ghost morph, matching its property-panel toggle.</summary>
        private static string GhostMorphLabel(GhostMorph morph)
        {
            return morph switch
            {
                GhostMorph.Grab => Localizer.AttributeName("grab"),
                GhostMorph.Bubble => Localizer.AttributeName("bubble"),
                GhostMorph.Bouncer => Localizer.AttributeName("bouncer"),
                _ => "?",
            };
        }
    }
}

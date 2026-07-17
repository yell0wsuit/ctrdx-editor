using System;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Pure placement data for the mechanical hand's terminal claw.</summary>
    /// <param name="Sprite">Atlas source and unrotated level-space destination.</param>
    /// <param name="Pivot">Terminal joint around which the claw inherits rotation.</param>
    /// <param name="AngleDegrees">Final segment's absolute world angle.</param>
    public readonly record struct HandClawPlacement(SpriteLayout Sprite, Vec2 Pivot, double AngleDegrees);

    /// <summary>Computes the game-authored terminal claw transform without requiring a drawing backend.</summary>
    public static class HandClawLayout
    {
        /// <summary>Places the claw's untrimmed source box on the terminal joint and resolves inherited rotation.</summary>
        public static HandClawPlacement Compute(AtlasFrame frame, LevelObject hand)
        {
            int count = HandObject.SegmentCount(hand);
            Vec2 pivot = HandGeometry.ClawPosition(hand);
            double angle = count > 0 ? HandObject.Angle(hand, count) : 0;
            return new HandClawPlacement(
                SpritePlacement.Compute(frame, pivot.X, pivot.Y),
                pivot,
                angle);
        }
    }

    /// <summary>
    /// Draws a mechanical hand from the `hand_parts` strip: the base on joint 0, a tiled bone per segment,
    /// a joint marker per segment origin, and the idle claw on the last joint.
    ///
    /// Marker placement follows the game: <c>MechanicalHandSegment.Update</c> pins each segment's button to
    /// its own <c>drawX/drawY</c> — the segment's origin — so segment i's marker sits on joint i-1, and the
    /// terminal joint carries the claw instead. The game's <c>clawOffset</c> is not modeled here; it places
    /// the runtime candy-attach point, while the claw visual is anchored on the joint.
    /// </summary>
    internal static class HandRenderer
    {
        private const int PartButtonIdle = 0;
        private const int PartButtonNone = 1;
        private const int PartBone = 2;
        private const int PartBase = 3;
        private const int PartClaw = 4;

        /// <summary>Draws <paramref name="hand"/> using the hand_parts pieces.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the hand pieces.</param>
        /// <param name="hand">The mechanical hand to draw.</param>
        public static void Draw(DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject hand)
        {
            if (!HandObject.IsHand(hand.Type))
            {
                return;
            }
            if (sprites.GetSprite("hand_parts", 0, 0) is not { Layers.Count: >= 5 } parts)
            {
                return;
            }

            Vec2[] joints = HandGeometry.Joints(hand);
            double z = v.Zoom;

            DrawCentered(ctx, parts.Layers[PartBase], v.LevelToScreen(joints[0]), z);

            for (int i = 1; i < joints.Length; i++)
            {
                DrawBone(
                    ctx,
                    parts.Layers[PartBone],
                    v.LevelToScreen(joints[i - 1]),
                    HandObject.Angle(hand, i),
                    HandObject.Length(hand, i),
                    z);
            }

            for (int i = 1; i < joints.Length; i++)
            {
                int part = HandObject.Rotatable(hand, i) ? PartButtonIdle : PartButtonNone;
                DrawCentered(ctx, parts.Layers[part], v.LevelToScreen(joints[i - 1]), z);
            }

            DrawClaw(ctx, v, parts.Layers[PartClaw], hand);
        }

        private static void DrawCentered(DrawingContext ctx, SpriteLayerDraw layer, Vec2 center, double z)
        {
            IntRect source = layer.Frame.Frame;
            if (source.W <= 0 || source.H <= 0)
            {
                return;
            }

            double w = source.W / SpritePlacement.MapScale * z;
            double h = source.H / SpritePlacement.MapScale * z;
            ctx.DrawImage(
                layer.Bitmap,
                SourceRect(source),
                new Rect(center.X - (w / 2), center.Y - (h / 2), w, h));
        }

        private static void DrawClaw(DrawingContext ctx, ViewTransform v, SpriteLayerDraw layer, LevelObject hand)
        {
            if (layer.Frame.Frame.W <= 0 || layer.Frame.Frame.H <= 0)
            {
                return;
            }

            HandClawPlacement placement = HandClawLayout.Compute(layer.Frame, hand);
            Vec2 pivot = v.LevelToScreen(placement.Pivot);
            LevelBounds dest = placement.Sprite.Dest;
            Rect localDest = new(
                (dest.X - placement.Pivot.X) * v.Zoom,
                (dest.Y - placement.Pivot.Y) * v.Zoom,
                dest.W * v.Zoom,
                dest.H * v.Zoom);
            Matrix transform = Matrix.CreateRotation(placement.AngleDegrees * Math.PI / 180)
                * Matrix.CreateTranslation(pivot.X, pivot.Y);
            using (ctx.PushTransform(transform))
            {
                ctx.DrawImage(layer.Bitmap, SourceRect(placement.Sprite.Source), localDest);
            }
        }

        // The game stretches a TiledImage of the bone quad along the segment (armImage.width = length) in a
        // local space whose +X runs along the segment, then rotates it by the segment's angle. The same is
        // reproduced here by repeating the quad along +X under a rotate-about-origin transform, clipping the
        // final partial tile so the bone ends exactly on the joint.
        private static void DrawBone(
            DrawingContext ctx, SpriteLayerDraw layer, Vec2 origin, double angleDeg, double length, double z)
        {
            IntRect source = layer.Frame.Frame;
            if (source.W <= 0 || source.H <= 0 || length <= 0)
            {
                return;
            }

            double tileLength = source.W / SpritePlacement.MapScale;
            double thickness = source.H / SpritePlacement.MapScale;
            if (tileLength <= 0)
            {
                return;
            }

            Matrix m = Matrix.CreateRotation(angleDeg * Math.PI / 180)
                * Matrix.CreateTranslation(origin.X, origin.Y);
            using (ctx.PushTransform(m))
            {
                for (double along = 0; along < length - 0.0001; along += tileLength)
                {
                    double visible = Math.Min(tileLength, length - along);
                    double fraction = visible / tileLength;
                    Rect dest = new(along * z, -thickness / 2 * z, visible * z, thickness * z);
                    Rect src = new(source.X, source.Y, source.W * fraction, source.H);
                    ctx.DrawImage(layer.Bitmap, src, dest);
                }
            }
        }

        private static Rect SourceRect(IntRect source)
        {
            return new Rect(source.X, source.Y, source.W, source.H);
        }
    }
}

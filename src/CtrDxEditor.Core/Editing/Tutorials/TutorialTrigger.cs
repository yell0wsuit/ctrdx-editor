using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Named tutorial conditions accepted by the map XML schema, in the game's order.</summary>
    public enum TutorialEvent
    {
        /// <summary>The level has finished loading its tutorial prompts.</summary>
        Start,
        /// <summary>A candy has entered a bubble.</summary>
        BubbleCapture,
        /// <summary>A bubble owned by a candy has popped.</summary>
        BubblePop,
        /// <summary>A lantern has captured a candy.</summary>
        LanternCatch,
        /// <summary>A sock has accepted a candy for transport.</summary>
        SockCatch,
        /// <summary>A mouse has grabbed a candy.</summary>
        MouseGrab,
        /// <summary>A spider has stolen a candy.</summary>
        SpiderSteal,
        /// <summary>A mechanical hand has captured a candy.</summary>
        HandGrab,
        /// <summary>A rope attached to a candy has been cut.</summary>
        RopeCut,
        /// <summary>A candy has collected a star.</summary>
        StarCollected,
        /// <summary>A target has eaten a candy.</summary>
        CandyEaten,
        /// <summary>A candy has entered a bamboo pipe.</summary>
        PipeEnter,
        /// <summary>A candy has hit ordinary spikes.</summary>
        SpikeHit,
        /// <summary>A candy has hit electro spikes.</summary>
        ElectroHit,
        /// <summary>The level has entered its won outcome.</summary>
        GameWon,
        /// <summary>The level has entered its lost outcome.</summary>
        GameLost,
        /// <summary>A rocket has transitioned into flight.</summary>
        RocketIgnite,
        /// <summary>A candy has collided with an active bouncer.</summary>
        BouncerHit,
        /// <summary>The player has operated a pump.</summary>
        PumpFire,
        /// <summary>The player has activated a steam tube.</summary>
        SteamBurst,
        /// <summary>The player has started rotating a disc.</summary>
        DiscSpin,
        /// <summary>The player has frozen time.</summary>
        TimeFreeze,
        /// <summary>The player has resumed time.</summary>
        TimeUnfreeze,
        /// <summary>The player has toggled gravity.</summary>
        GravityFlip,
        /// <summary>A candy currently occupies a bubble.</summary>
        Bubbled,
        /// <summary>A candy is currently held by a lantern.</summary>
        InLantern,
        /// <summary>A candy is currently carried by an ant.</summary>
        CarriedByAnt,
        /// <summary>A candy is currently carried by a snail.</summary>
        CarriedBySnail,
        /// <summary>Time is currently frozen.</summary>
        TimeFrozen,
        /// <summary>Gravity is currently inverted.</summary>
        GravityInverted,
        /// <summary>A candy occupies the required authored region.</summary>
        CandyMoved,
    }

    /// <summary>Whether an event fires at a transition or holds while a state is true.</summary>
    public enum TutorialEventKind
    {
        /// <summary>The event occurs at a single authoritative transition.</summary>
        Edge,
        /// <summary>The event remains true while its authoritative state holds.</summary>
        State,
    }

    /// <summary>Parses and classifies the closed tutorial event vocabulary.</summary>
    public static class TutorialEvents
    {
        /// <summary>Parses an exact, case-sensitive XML event name; null reads as start.</summary>
        public static bool TryParse(string? value, out TutorialEvent result)
        {
            switch (value)
            {
                case null or "start": result = TutorialEvent.Start; return true;
                case "bubbleCapture": result = TutorialEvent.BubbleCapture; return true;
                case "bubblePop": result = TutorialEvent.BubblePop; return true;
                case "lanternCatch": result = TutorialEvent.LanternCatch; return true;
                case "sockCatch": result = TutorialEvent.SockCatch; return true;
                case "mouseGrab": result = TutorialEvent.MouseGrab; return true;
                case "spiderSteal": result = TutorialEvent.SpiderSteal; return true;
                case "handGrab": result = TutorialEvent.HandGrab; return true;
                case "ropeCut": result = TutorialEvent.RopeCut; return true;
                case "starCollected": result = TutorialEvent.StarCollected; return true;
                case "candyEaten": result = TutorialEvent.CandyEaten; return true;
                case "pipeEnter": result = TutorialEvent.PipeEnter; return true;
                case "spikeHit": result = TutorialEvent.SpikeHit; return true;
                case "electroHit": result = TutorialEvent.ElectroHit; return true;
                case "gameWon": result = TutorialEvent.GameWon; return true;
                case "gameLost": result = TutorialEvent.GameLost; return true;
                case "rocketIgnite": result = TutorialEvent.RocketIgnite; return true;
                case "bouncerHit": result = TutorialEvent.BouncerHit; return true;
                case "pumpFire": result = TutorialEvent.PumpFire; return true;
                case "steamBurst": result = TutorialEvent.SteamBurst; return true;
                case "discSpin": result = TutorialEvent.DiscSpin; return true;
                case "timeFreeze": result = TutorialEvent.TimeFreeze; return true;
                case "timeUnfreeze": result = TutorialEvent.TimeUnfreeze; return true;
                case "gravityFlip": result = TutorialEvent.GravityFlip; return true;
                case "bubbled": result = TutorialEvent.Bubbled; return true;
                case "inLantern": result = TutorialEvent.InLantern; return true;
                case "carriedByAnt": result = TutorialEvent.CarriedByAnt; return true;
                case "carriedBySnail": result = TutorialEvent.CarriedBySnail; return true;
                case "timeFrozen": result = TutorialEvent.TimeFrozen; return true;
                case "gravityInverted": result = TutorialEvent.GravityInverted; return true;
                case "candyMoved": result = TutorialEvent.CandyMoved; return true;
                default: result = TutorialEvent.Start; return false;
            }
        }

        /// <summary>The exact XML spelling for an event.</summary>
        public static string Name(TutorialEvent value) => value switch
        {
            TutorialEvent.Start => "start",
            TutorialEvent.BubbleCapture => "bubbleCapture",
            TutorialEvent.BubblePop => "bubblePop",
            TutorialEvent.LanternCatch => "lanternCatch",
            TutorialEvent.SockCatch => "sockCatch",
            TutorialEvent.MouseGrab => "mouseGrab",
            TutorialEvent.SpiderSteal => "spiderSteal",
            TutorialEvent.HandGrab => "handGrab",
            TutorialEvent.RopeCut => "ropeCut",
            TutorialEvent.StarCollected => "starCollected",
            TutorialEvent.CandyEaten => "candyEaten",
            TutorialEvent.PipeEnter => "pipeEnter",
            TutorialEvent.SpikeHit => "spikeHit",
            TutorialEvent.ElectroHit => "electroHit",
            TutorialEvent.GameWon => "gameWon",
            TutorialEvent.GameLost => "gameLost",
            TutorialEvent.RocketIgnite => "rocketIgnite",
            TutorialEvent.BouncerHit => "bouncerHit",
            TutorialEvent.PumpFire => "pumpFire",
            TutorialEvent.SteamBurst => "steamBurst",
            TutorialEvent.DiscSpin => "discSpin",
            TutorialEvent.TimeFreeze => "timeFreeze",
            TutorialEvent.TimeUnfreeze => "timeUnfreeze",
            TutorialEvent.GravityFlip => "gravityFlip",
            TutorialEvent.Bubbled => "bubbled",
            TutorialEvent.InLantern => "inLantern",
            TutorialEvent.CarriedByAnt => "carriedByAnt",
            TutorialEvent.CarriedBySnail => "carriedBySnail",
            TutorialEvent.TimeFrozen => "timeFrozen",
            TutorialEvent.GravityInverted => "gravityInverted",
            TutorialEvent.CandyMoved => "candyMoved",
            _ => "start",
        };

        /// <summary>Bubbled and later are continuously observable states.</summary>
        public static TutorialEventKind Kind(TutorialEvent value)
        {
            return value >= TutorialEvent.Bubbled ? TutorialEventKind.State : TutorialEventKind.Edge;
        }

        /// <summary>Every event once, in declaration order, for the properties dropdown.</summary>
        public static IReadOnlyList<TutorialEvent> All { get; } = Enum.GetValues<TutorialEvent>();
    }

    /// <summary>
    /// Resolves the localization key for a tutorial prompt's on-canvas trigger badge (drawn by
    /// LevelCanvas, in CtrDxEditor.Shared). Preview fires every prompt at t=0 regardless of what would
    /// really trigger it - the editor has no simulation - so this badge is what keeps that simplification
    /// honest: it tells the level author what the prompt is actually waiting for.
    /// </summary>
    public static class TutorialBadge
    {
        /// <summary>Key for a prompt gated on a single-transition event, e.g. "on rope cut".</summary>
        public const string EdgeKey = "Canvas.Tutorial.Badge.Edge";

        /// <summary>Key for a prompt gated on a held condition, e.g. "while bubbled".</summary>
        public const string StateKey = "Canvas.Tutorial.Badge.State";

        /// <summary>Key for a start prompt whose only annotation-worthy fact is an authored delay.</summary>
        public const string DelayKey = "Canvas.Tutorial.Badge.Delay";

        /// <summary>Key for a start prompt whose only annotation-worthy fact is an authored sequencing group.</summary>
        public const string GroupKey = "Canvas.Tutorial.Badge.Group";

        /// <summary>
        /// Key for a prompt whose <c>showOn</c> failed to parse. The game's loader
        /// (TutorialPromptLoader.LoadAll, called with skipInvalid: true) catches exactly this failure per
        /// prompt and drops the whole prompt - it never plays at all - so this must never be confused with
        /// a start prompt, which plays immediately.
        /// </summary>
        public const string InvalidKey = "Canvas.Tutorial.Badge.Invalid";

        /// <summary>
        /// The localization key for <paramref name="o"/>'s trigger badge, or null when a start prompt with
        /// no delay and no group needs none - it already plays exactly when the t=0 preview shows it, so
        /// there is nothing to correct.
        /// </summary>
        /// <remarks>
        /// A <c>showOn</c> that fails to parse takes priority over every other check and returns
        /// <see cref="InvalidKey"/>: unlike a null or <c>"start"</c> value (both of which
        /// <see cref="TutorialEvents.TryParse"/> accepts as <see cref="TutorialEvent.Start"/>), an
        /// authored-but-unrecognized value makes the game's loader drop the prompt entirely, so delay and
        /// group are moot - the prompt never plays regardless of what else is authored on it. The validator
        /// separately flags the same value as an error the game would drop the prompt for; this badge does
        /// not duplicate that message, only the on-canvas consequence of it.
        /// </remarks>
        public static string? KeyFor(LevelObject o)
        {
            if (!TutorialEvents.TryParse(o.GetAttr("showOn"), out TutorialEvent showOn))
            {
                return InvalidKey;
            }

            if (showOn != TutorialEvent.Start)
            {
                return TutorialEvents.Kind(showOn) == TutorialEventKind.State ? StateKey : EdgeKey;
            }

            if (TutorialTiming.For(o).Delay > 0)
            {
                return DelayKey;
            }

            return string.IsNullOrEmpty(o.GetAttr("group")) ? null : GroupKey;
        }
    }

    /// <summary>Selects which active candy body may satisfy a tutorial trigger.</summary>
    public enum TutorialSubject
    {
        /// <summary>Any eligible candy body.</summary>
        Any,
        /// <summary>Any active body owned by the primary authored candy.</summary>
        Primary,
        /// <summary>The left half of a split candy.</summary>
        Left,
        /// <summary>The right half of a split candy.</summary>
        Right,
    }

    /// <summary>Parses the closed tutorial subject vocabulary.</summary>
    public static class TutorialSubjects
    {
        /// <summary>Parses an exact, case-sensitive XML subject name; null reads as any.</summary>
        public static bool TryParse(string? value, out TutorialSubject result)
        {
            switch (value)
            {
                case null or "any": result = TutorialSubject.Any; return true;
                case "primary": result = TutorialSubject.Primary; return true;
                case "left": result = TutorialSubject.Left; return true;
                case "right": result = TutorialSubject.Right; return true;
                default: result = TutorialSubject.Any; return false;
            }
        }

        /// <summary>The exact XML spelling for a subject.</summary>
        public static string Name(TutorialSubject value) => value switch
        {
            TutorialSubject.Any => "any",
            TutorialSubject.Primary => "primary",
            TutorialSubject.Left => "left",
            TutorialSubject.Right => "right",
            _ => "any",
        };
    }

    /// <summary>A tutorial trigger rectangle in map coordinates.</summary>
    public readonly record struct TutorialArea(double X, double Y, double Width, double Height)
    {
        /// <summary>Parses four finite comma-separated components with positive dimensions.</summary>
        public static bool TryParse(string? value, out TutorialArea area)
        {
            area = default;
            string[]? parts = value?.Split(',');
            if (parts is not { Length: 4 })
            {
                return false;
            }

            if (!Component(parts[0], out double x) || !Component(parts[1], out double y)
                || !Component(parts[2], out double width) || !Component(parts[3], out double height)
                || width <= 0 || height <= 0)
            {
                return false;
            }

            area = new TutorialArea(x, y, width, height);
            return true;
        }

        /// <summary>
        /// Parses an authored area and projects its four raw components through the integer-coordinate
        /// conversion DX applies when it instantiates the runtime trigger. A schema-valid positive
        /// fractional dimension can therefore become zero here, exactly as it does in the game.
        /// </summary>
        public static bool TryParseRuntime(string? value, out TutorialArea area)
        {
            area = default;
            if (!TryParse(value, out _))
            {
                return false;
            }

            string[] parts = value!.Split(',');
            area = new TutorialArea(
                RuntimeComponent(parts[0]),
                RuntimeComponent(parts[1]),
                RuntimeComponent(parts[2]),
                RuntimeComponent(parts[3]));
            return true;
        }

        /// <summary>Serializes back to the authored x,y,width,height spelling.</summary>
        public string Format()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{X:0.###},{Y:0.###},{Width:0.###},{Height:0.###}");
        }

        private static bool Component(string part, out double value)
        {
            if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float gameValue)
                && float.IsFinite(gameValue))
            {
                value = gameValue;
                return true;
            }

            value = 0;
            return false;
        }

        private static int RuntimeComponent(string part)
        {
            return !decimal.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsed)
                || parsed < int.MinValue
                || parsed > int.MaxValue
                    ? 0
                    : decimal.ToInt32(decimal.Truncate(parsed));
        }
    }

    /// <summary>
    /// Pure canvas geometry for dragging a <see cref="TutorialArea"/> by its corners. The rectangle has no
    /// rotation, so a drag only ever moves the two edges meeting at the dragged corner; the opposite corner
    /// is the pivot that stays put. Kept in Core, apart from the canvas hit-testing/drawing, so the drag
    /// math is unit-testable without a UI.
    /// </summary>
    public static class TutorialAreaResize
    {
        /// <summary>Smallest width/height a drag may produce, so a corner dragged onto its neighbor never
        /// collapses the area to a zero-size rectangle that <see cref="TutorialArea.TryParse"/> would reject.</summary>
        private const double MinSize = 1;

        /// <summary>The area's four corners in map coordinates, clockwise from top-left.</summary>
        /// <param name="area">The trigger area.</param>
        /// <returns>Corners in order: top-left (0), top-right (1), bottom-right (2), bottom-left (3).</returns>
        public static Vec2[] Corners(TutorialArea area)
        {
            return
            [
                new Vec2(area.X, area.Y),
                new Vec2(area.X + area.Width, area.Y),
                new Vec2(area.X + area.Width, area.Y + area.Height),
                new Vec2(area.X, area.Y + area.Height),
            ];
        }

        /// <summary>Returns the corner index (0..3, see <see cref="Corners"/>) under a point, or -1.</summary>
        /// <param name="area">The trigger area.</param>
        /// <param name="point">The point to test, in map coordinates.</param>
        /// <param name="tolerance">The hit radius, in the same units as <paramref name="point"/>.</param>
        public static int HitCorner(TutorialArea area, Vec2 point, double tolerance)
        {
            Vec2[] corners = Corners(area);
            double toleranceSquared = tolerance * tolerance;
            for (int i = 0; i < corners.Length; i++)
            {
                double dx = corners[i].X - point.X;
                double dy = corners[i].Y - point.Y;
                if ((dx * dx) + (dy * dy) <= toleranceSquared)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Moves one corner to a new position, recomputing the rectangle from the dragged corner and its
        /// fixed opposite. The result always has positive dimensions: dragging past the opposite corner
        /// flips which side is which rather than inverting width or height.
        /// </summary>
        /// <param name="area">The area before the drag.</param>
        /// <param name="corner">The corner index being dragged, 0..3 clockwise from top-left (see <see cref="Corners"/>).</param>
        /// <param name="to">The corner's new position, in map coordinates.</param>
        /// <returns>The area recomputed from the dragged corner and the fixed opposite corner.</returns>
        public static TutorialArea DragCorner(TutorialArea area, int corner, Vec2 to)
        {
            to = new Vec2(
                Math.Round(to.X, MidpointRounding.AwayFromZero),
                Math.Round(to.Y, MidpointRounding.AwayFromZero));
            Vec2 opposite = Corners(area)[(corner + 2) % 4];
            double minX = Math.Min(opposite.X, to.X);
            double maxX = Math.Max(opposite.X, to.X);
            double minY = Math.Min(opposite.Y, to.Y);
            double maxY = Math.Max(opposite.Y, to.Y);
            return new TutorialArea(minX, minY, Math.Max(MinSize, maxX - minX), Math.Max(MinSize, maxY - minY));
        }
    }
}

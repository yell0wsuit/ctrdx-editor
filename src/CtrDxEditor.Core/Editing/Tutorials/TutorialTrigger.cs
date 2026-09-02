using System;
using System.Collections.Generic;
using System.Globalization;

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
    }
}

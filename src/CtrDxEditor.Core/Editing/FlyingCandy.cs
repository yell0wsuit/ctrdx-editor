using System;
using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Time Travel's flying candy: a <c>&lt;candy isDriven="true"&gt;</c> that grows wings and copies
    /// every move of the candy it follows. Mirrors the game's <c>GameScene.FlyingCandy</c>.
    /// </summary>
    public static class FlyingCandy
    {
        /// <summary>The candy attribute that makes it fly.</summary>
        public const string Attribute = "isDriven";

        /// <summary>Number of wing-flap frames (game <c>CandyFlightDefinition</c> quads 0-3).</summary>
        public const int FlapFrameCount = 4;

        /// <summary>Seconds per wing-flap frame (game <c>CandyFlightDefinition.FlapFrameDelay</c>).</summary>
        public const double FlapFrameDelay = 0.02;

        /// <summary>Whether a candy is authored to fly, parsed like the game's <c>GetBoolAttribute</c>.</summary>
        /// <param name="obj">Object to inspect.</param>
        /// <returns><see langword="true"/> for a <c>candy</c> whose <c>isDriven</c> is <c>true</c> (any case) or <c>1</c>.</returns>
        public static bool IsDriven(LevelObject obj)
        {
            if (obj.Type != "candy")
            {
                return false;
            }
            string? value = obj.GetAttr(Attribute);
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        /// <summary>
        /// Whether the level has something a flying candy can follow: an edible candy that does not fly
        /// itself - a plain <c>candy</c>, or the split candy a <c>candyL</c>/<c>candyR</c> pair forms in
        /// a <c>twoParts</c> level. Mirrors the game's <c>FlyingCandyLeaderFor</c>.
        /// </summary>
        /// <param name="objects">All level objects.</param>
        /// <param name="twoParts">The level's <c>twoParts</c> setting, which the split candy needs.</param>
        /// <returns><see langword="true"/> when a leader exists.</returns>
        public static bool HasLeader(IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            bool splitCandy = twoParts && objects.Any(o => o.Type == "candyL") && objects.Any(o => o.Type == "candyR");
            return splitCandy || objects.Any(o => o.Type == "candy" && !IsDriven(o));
        }

        /// <summary>
        /// Whether the game gives this candy wings: it is authored to fly and has a leader. Without one
        /// the game plays it as an ordinary candy.
        /// </summary>
        /// <param name="obj">Candidate flying candy.</param>
        /// <param name="objects">All level objects.</param>
        /// <param name="twoParts">The level's <c>twoParts</c> setting.</param>
        /// <returns><see langword="true"/> when the candy flies in game.</returns>
        public static bool HasWings(LevelObject obj, IReadOnlyList<LevelObject> objects, bool twoParts)
        {
            return IsDriven(obj) && HasLeader(objects, twoParts);
        }

        /// <summary>Static wing frame, or the game's looping 0-3 flap during animation preview.</summary>
        /// <param name="elapsedSeconds">Elapsed animation-preview time, or null for the static first frame.</param>
        /// <returns>The flap frame index, 0 to <see cref="FlapFrameCount"/> - 1.</returns>
        public static int FlapFrame(double? elapsedSeconds)
        {
            return elapsedSeconds is double seconds
                ? (int)Math.Floor(Math.Max(0, seconds) / FlapFrameDelay) % FlapFrameCount
                : 0;
        }

        /// <summary>Visual descriptor key for one wing-flap frame (flapping wings + wing root).</summary>
        /// <param name="frame">Flap frame index from <see cref="FlapFrame"/>.</param>
        /// <returns>The sprite key.</returns>
        public static string WingSpriteKey(int frame)
        {
            return $"candy_wings_{frame}";
        }
    }
}

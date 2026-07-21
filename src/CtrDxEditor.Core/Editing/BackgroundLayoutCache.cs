using System;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Memoizes a <see cref="BackgroundLayout"/> across frames. The canvas recomputes the background on every
    /// <c>InvalidateVisual</c>, but <see cref="BackgroundPlacement.Compute"/> is pure and its inputs change
    /// only when the document or the active background does, so recomputing per frame is wasted work.
    /// </summary>
    /// <remarks>
    /// Holds a single entry rather than a map: the inputs change rarely and never alternate, so there is
    /// nothing to thrash against and an unbounded map would only leak.
    /// </remarks>
    public sealed class BackgroundLayoutCache
    {
        private (double Width, double Height, int Background, double P1Aspect, double P2Aspect)? _key;
        private BackgroundLayout _layout;

        /// <summary>Returns the layout for these inputs, computing it only when they differ from last time.</summary>
        /// <remarks>
        /// The art aspects are part of the key, not just the level size and background index: swapping the
        /// sprite cache (content setup completing mid-session) can change the art behind an unchanged index,
        /// and serving a stale layout there would be silent.
        /// </remarks>
        /// <param name="levelWidth">Level width in level units.</param>
        /// <param name="levelHeight">Level height in level units.</param>
        /// <param name="background">Active background index.</param>
        /// <param name="p1Aspect">Primary background art aspect (height / width).</param>
        /// <param name="p2Aspect">Secondary background art aspect (height / width), or zero when absent.</param>
        /// <param name="compute">Computes the layout on a miss.</param>
        /// <returns>The cached layout, or a freshly computed one when the inputs changed.</returns>
        public BackgroundLayout Get(
            double levelWidth,
            double levelHeight,
            int background,
            double p1Aspect,
            double p2Aspect,
            Func<BackgroundLayout> compute)
        {
            (double, double, int, double, double) key =
                (levelWidth, levelHeight, background, p1Aspect, p2Aspect);
            if (_key != key)
            {
                _layout = compute();
                _key = key;
            }

            return _layout;
        }
    }
}

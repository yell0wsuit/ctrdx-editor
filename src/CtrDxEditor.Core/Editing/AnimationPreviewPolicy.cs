using System;
using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Determines whether an object contains data supported by live animation preview.</summary>
    public static class AnimationPreviewPolicy
    {
        /// <summary>Scrubber cap for a tutorial hold long enough to make the preview useless otherwise.</summary>
        private const double MaxTutorialPreviewSeconds = 60.0;

        /// <summary>Returns whether the level contains any data that can visibly animate.</summary>
        public static bool CanPreview(LevelDocument document)
        {
            return (document.Water > 0f && document.WaterSpeed > 0f)
                || document.AllObjects.Any(CanPreview);
        }

        /// <summary>Returns whether the object can visibly animate during live preview.</summary>
        public static bool CanPreview(LevelObject obj)
        {
            return ElectroAnimation.HasActiveTiming(obj)
                || (SpinTable.IsSpinnable(obj.Type) && ObjectSpin.IsRotatingInPlace(obj))
                || HasVisibleMovement(obj)
                // A tutorial prompt always fades (fadeIn/fadeOut default rather than opt in), so every
                // prompt is previewable whether or not it also authors motion.
                || TutorialObject.IsText(obj.Type)
                || TutorialObject.IsImage(obj.Type);
        }

        /// <summary>
        /// The longest finite pass any tutorial prompt in the document authors, clamped so a very long
        /// hold cannot make the scrubber useless, or <see langword="null"/> when every prompt is
        /// unbounded (a forever hold or a forever repeat) and so contributes no finite length.
        /// </summary>
        public static double? TutorialPreviewSeconds(LevelDocument document)
        {
            double? longest = null;
            foreach (LevelObject obj in document.AllObjects)
            {
                if (!TutorialObject.IsText(obj.Type) && !TutorialObject.IsImage(obj.Type))
                {
                    continue;
                }

                if (TutorialTiming.For(obj).TotalSeconds is double seconds
                    && (longest is not double current || seconds > current))
                {
                    longest = seconds;
                }
            }

            return longest is double result ? Math.Min(result, MaxTutorialPreviewSeconds) : null;
        }

        private static bool HasVisibleMovement(LevelObject obj)
        {
            if (!MoverPath.HasActiveMovement(obj))
            {
                return false;
            }

            string? path = obj.GetAttr("path");
            return MoverPath.IsCircularPath(path)
                ? MoverPath.CircularRadius(path, 0) > 0
                : MoverPath.IsPolylineMovement(path);
        }
    }
}

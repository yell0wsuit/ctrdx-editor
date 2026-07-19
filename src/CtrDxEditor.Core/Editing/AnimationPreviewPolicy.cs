using System.Linq;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Determines whether an object contains data supported by live animation preview.</summary>
    public static class AnimationPreviewPolicy
    {
        /// <summary>Returns whether the level contains any data that can visibly animate.</summary>
        public static bool CanPreview(LevelDocument document)
        {
            return (document.Water > 0f && document.WaterSpeed > 0f)
                || document.AllObjects.Any(CanPreview);
        }

        /// <summary>Returns whether the object can visibly animate during live preview.</summary>
        public static bool CanPreview(LevelObject obj)
        {
            return ElectroAnimation.IsElectro(obj.Type)
                || ObjectSpin.IsRotatingInPlace(obj)
                || MoverPath.HasActiveMovement(obj);
        }
    }
}

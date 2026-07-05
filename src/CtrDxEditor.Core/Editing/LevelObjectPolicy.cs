using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Level-aware rules for object defaults and editable attribute availability.</summary>
    public static class LevelObjectPolicy
    {
        /// <summary>Applies defaults that depend on the active level settings.</summary>
        public static void ApplyDefaults(LevelObject obj, LevelDocument document)
        {
            if (document.TwoParts && obj.Type == "grab" && obj.GetAttr("part") is null)
            {
                obj.SetAttr("part", "L");
            }
        }

        /// <summary>Returns whether an object attribute should be exposed for editing in this level.</summary>
        public static bool IsAttributeVisible(string element, string attribute, LevelDocument document)
        {
            return element != "grab" || attribute != "part" || document.TwoParts;
        }
    }
}

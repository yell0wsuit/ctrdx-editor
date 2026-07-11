using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for bouncers, stored as bouncer1 or bouncer2 according to their width.</summary>
    public static class BouncerObject
    {
        /// <summary>Whether an element is one of the two bouncer XML elements accepted by DX.</summary>
        public static bool IsBouncer(string element)
        {
            return element is "bouncer1" or "bouncer2";
        }

        /// <summary>Returns size 1 or 2 from a valid attribute, falling back to the element suffix.</summary>
        public static string Size(LevelObject obj)
        {
            string? attribute = obj.GetAttr("size");
            return IsValidSize(attribute) ? attribute! : obj.Type == "bouncer2" ? "2" : "1";
        }

        /// <summary>Changes the width and synchronizes the backing bouncerN element name.</summary>
        public static void SetSize(LevelObject obj, string? size)
        {
            if (!IsValidSize(size))
            {
                return;
            }

            obj.Element.Name = $"bouncer{size}";
            obj.SetAttr("size", size!);
        }

        private static bool IsValidSize(string? size)
        {
            return size is "1" or "2";
        }
    }
}

using System.Globalization;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for DX's magic-hat teleporter, stored as a <c>sock</c> XML element.</summary>
    public static class SockObject
    {
        /// <summary>Returns the visual key selected by the Christmas event and transporter group.</summary>
        /// <param name="obj">Magic-hat level object.</param>
        /// <param name="isXmas">Whether DX's Christmas event is active.</param>
        /// <returns>A key choosing the normal or Christmas atlas and group quad.</returns>
        public static string SpriteKey(LevelObject obj, bool isXmas)
        {
            bool grouped = int.TryParse(
                obj.GetAttr("group"),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int group) && group != 0;

            return (isXmas, grouped) switch
            {
                (false, false) => "sock",
                (false, true) => "sock_grouped",
                (true, false) => "sock_xmas",
                (true, true) => "sock_xmas_grouped",
            };
        }
    }
}

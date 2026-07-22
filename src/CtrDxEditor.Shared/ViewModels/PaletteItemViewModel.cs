using Avalonia.Media;

using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Palette entry for one placeable object type.</summary>
    public sealed partial class PaletteItemViewModel(string element, string displayName, bool enabled, IImage? icon, string groupName)
        : ViewModelBase
    {
        /// <summary>The raw object element name to place.</summary>
        public string Element { get; } = element;

        /// <summary>The localized display name shown in the palette.</summary>
        public string DisplayName { get; } = displayName;

        /// <summary>The localized game/group label this item belongs to.</summary>
        public string GroupName { get; } = groupName;

        /// <summary>True when this item is the first visible item of its group (renders a section header).</summary>
        public bool ShowGroupHeader
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>True when this item opens a group other than the first visible one (renders a divider above).</summary>
        public bool ShowDivider
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>Small sprite preview shown in the palette.</summary>
        public IImage? Icon { get; } = icon;

        /// <summary>Whether the icon receives the white alpha-mask overlay in the dark theme.</summary>
        public bool InvertOnDarkTheme => TutorialObject.IsImage(Element)
            && TutorialObject.ShouldInvert(TutorialObject.QuadForTag(Element), dark: true);

        /// <summary>Whether the palette item can currently be placed.</summary>
        public bool Enabled
        {
            get;
            set => SetProperty(ref field, value);
        } = enabled;

        /// <summary>True while this item is the one being dragged from the palette.</summary>
        public bool IsDragging
        {
            get;
            set => SetProperty(ref field, value);
        }

        /// <summary>True for a moment after this item placed an object, driving the row's confirmation.</summary>
        /// <remarks>
        /// A palette tap drops the object at the level centre, which in the compact shell sits behind the
        /// drawer. With no cue on the row itself the tap reads as having failed, and users tap again —
        /// so the confirmation lives on the control that was touched rather than out on the canvas.
        /// </remarks>
        public bool JustPlaced
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}

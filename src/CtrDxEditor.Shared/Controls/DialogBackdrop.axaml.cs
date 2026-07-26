using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using DialogHostAvalonia;

namespace CtrDxEditor.Controls
{
    /// <summary>A full-window scrim inserted between stacked dialog sessions.</summary>
    public partial class DialogBackdrop : UserControl
    {
        /// <summary>Creates the backdrop and loads its XAML.</summary>
        public DialogBackdrop()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Inserts a backdrop immediately above an existing dialog popup host.</summary>
        /// <param name="parentHost">The popup host to dim, or null when no dialog is open.</param>
        /// <returns>The inserted backdrop, or null when there is no parent dialog surface.</returns>
        public static DialogBackdrop? InsertAfter(DialogOverlayPopupHost? parentHost)
        {
            if (parentHost?.GetVisualParent() is not Panel root)
            {
                return null;
            }

            int parentIndex = root.Children.IndexOf(parentHost);
            if (parentIndex < 0)
            {
                return null;
            }

            DialogBackdrop backdrop = new();
            root.Children.Insert(parentIndex + 1, backdrop);
            return backdrop;
        }

        /// <summary>Removes this backdrop from the dialog host visual tree.</summary>
        public void Detach()
        {
            if (this.GetVisualParent() is Panel root)
            {
                _ = root.Children.Remove(this);
            }
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = base.MeasureOverride(availableSize);
            return new Size(
                double.IsFinite(availableSize.Width) ? availableSize.Width : desiredSize.Width,
                double.IsFinite(availableSize.Height) ? availableSize.Height : desiredSize.Height);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>The object palette: searchable, grouped, with a sticky group header pinned while scrolling.</summary>
    public partial class PaletteView : UserControl
    {
        /// <summary>Initializes the palette panel.</summary>
        public PaletteView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The palette's item host, so the parent view can wire mouse drag-to-place onto it.
        /// </summary>
        /// <remarks>
        /// Exposed because <c>FindControl</c> does not cross into this control's name scope: <c>MainView</c>
        /// can find this control but not the <c>ItemsControl</c> inside it.
        /// </remarks>
        public ItemsControl ItemsHost => this.FindControl<ItemsControl>("PaletteList")!;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Moved verbatim from MainView.axaml.cs; the controls it reads now live in this name scope.
        private void OnPaletteScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (this.FindControl<ScrollViewer>("PaletteScroll") is not { } scroll
                || this.FindControl<ItemsControl>("PaletteList") is not { } list
                || this.FindControl<Border>("StickyHeaderHost") is not { } host
                || this.FindControl<TextBlock>("StickyHeaderText") is not { } text)
            {
                return;
            }

            string? topGroup = null;
            for (int i = 0; i < list.ItemCount; i++)
            {
                if (list.ContainerFromIndex(i) is not Control container)
                {
                    continue;
                }
                if (container.TranslatePoint(new Point(0, container.Bounds.Height), scroll) is not { } p)
                {
                    continue;
                }
                // First item whose bottom edge is below the top of the viewport owns the sticky header.
                if (p.Y > 0 && list.Items[i] is PaletteItemViewModel item)
                {
                    topGroup = item.GroupName;
                    break;
                }
            }

            bool scrolled = scroll.Offset.Y > 0.5;
            host.IsVisible = scrolled && topGroup is not null;
            if (topGroup is not null)
            {
                text.Text = topGroup;
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Views
{
    /// <summary>Resizable desktop host for the shared Usage Guide surface.</summary>
    public partial class UsageGuideWindow : Window
    {
        /// <summary>Creates the desktop guide window.</summary>
        public UsageGuideWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

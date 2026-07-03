using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Views
{
    /// <summary>Desktop window shell that hosts the editor <see cref="MainView"/>.</summary>
    public partial class MainWindow : Window
    {
        /// <summary>Creates the main editor window.</summary>
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

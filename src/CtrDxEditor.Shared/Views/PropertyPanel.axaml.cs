using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CtrDxEditor.Views
{
    /// <summary>Properties panel view for editing selected object attributes.</summary>
    public partial class PropertyPanel : UserControl
    {
        /// <summary>Creates the properties panel and loads its XAML.</summary>
        public PropertyPanel()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

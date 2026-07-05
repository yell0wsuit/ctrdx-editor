using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>New-level / edit-settings dialog. Returns the chosen settings, or empty on cancel.</summary>
    public partial class LevelSettingsDialog : BaseDialog<LevelSettings>
    {
        /// <summary>Creates the dialog and titles it for New vs Edit based on the view model mode.</summary>
        public LevelSettingsDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += (_, _) => ApplyMode();
        }

        private void ApplyMode()
        {
            if (DataContext is not LevelSettingsViewModel vm)
            {
                return;
            }
            TextBlock title = this.FindControl<TextBlock>("TitleText")!;
            Button confirm = this.FindControl<Button>("ConfirmButton")!;
            title.Text = Localizer.Get(vm.FlagsEditable ? "Dialog.LevelSettings.TitleNew" : "Dialog.LevelSettings.TitleEdit");
            confirm.Content = Localizer.Get(vm.FlagsEditable ? "Dialog.LevelSettings.Create" : "Dialog.LevelSettings.Save");
        }

        private void Confirm_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is LevelSettingsViewModel vm)
            {
                Close(vm.ToSettings());
            }
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

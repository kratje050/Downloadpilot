using System.Windows;
using DownloadPilot.App.Services;
using DownloadPilot.App.ViewModels;

namespace DownloadPilot.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ThemeManager.Apply(viewModel.SettingsEditor.Theme);

        viewModel.SettingsEditor.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsEditorViewModel.Theme))
            {
                ThemeManager.Apply(viewModel.SettingsEditor.Theme);
            }
        };

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        };
    }

    private void MailPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is System.Windows.Controls.PasswordBox passwordBox)
        {
            viewModel.MailPassword = passwordBox.Password;
        }
    }
}

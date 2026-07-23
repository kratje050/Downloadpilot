using System.ComponentModel;
using System.Windows;
using DownloadPilot.App.Services;
using DownloadPilot.App.ViewModels;

namespace DownloadPilot.App;

public partial class MainWindow : Window
{
    private bool _allowClose;

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

    public void AllowClose()
    {
        _allowClose = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
            return;
        }

        base.OnClosing(e);
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

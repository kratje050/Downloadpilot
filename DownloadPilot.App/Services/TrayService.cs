using System.Drawing;
using System.Windows;
using DownloadPilot.App.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace DownloadPilot.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly MainViewModel _viewModel;
    private readonly ProposalNotificationService _notificationService;
    private readonly NotifyIcon _notifyIcon;

    public TrayService(MainViewModel viewModel, ProposalNotificationService notificationService)
    {
        _viewModel = viewModel;
        _notificationService = notificationService;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "DownloadPilot"
        };

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("DownloadPilot openen", null, (_, _) => OpenMainWindow());
        menu.Items.Add("Bewaking pauzeren", null, (_, _) => _viewModel.StopMonitoringCommand.Execute(null));
        menu.Items.Add("Nieuwe bestanden bekijken", null, (_, _) => OpenMainWindow());
        menu.Items.Add("Opruimscan starten", null, (_, _) => _viewModel.ScanDownloadsCommand.Execute(null));
        menu.Items.Add("Laatste actie terugdraaien", null, (_, _) => _viewModel.UndoLastCommand.Execute(null));
        menu.Items.Add("Instellingen", null, (_, _) => OpenMainWindow());
        menu.Items.Add("Afsluiten", null, (_, _) => Application.Current.Shutdown());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => OpenMainWindow();
        _notificationService.ProposalBatchReady += OnProposalBatchReady;
    }

    public void ShowGroupedNotification(int count)
    {
        if (count <= 0)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "DownloadPilot";
        _notifyIcon.BalloonTipText = count == 1
            ? "Er is 1 nieuw voorstel klaar."
            : $"Er zijn {count} nieuwe voorstellen klaar.";
        _notifyIcon.ShowBalloonTip(1500);
    }

    public void Dispose()
    {
        _notificationService.ProposalBatchReady -= OnProposalBatchReady;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private void OnProposalBatchReady(object? sender, int count)
    {
        Application.Current.Dispatcher.BeginInvoke(() => ShowGroupedNotification(count));
    }

    private static void OpenMainWindow()
    {
        if (Application.Current.MainWindow is null)
        {
            return;
        }

        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.WindowState = WindowState.Normal;
        Application.Current.MainWindow.Activate();
    }
}

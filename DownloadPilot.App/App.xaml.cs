using System.Windows;
using DownloadPilot.App.Services;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DownloadPilot.App.ViewModels;
using DownloadPilot.Core.Abstractions;
using DownloadPilot.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application = System.Windows.Application;

namespace DownloadPilot.App;

public partial class App : Application
{
	private IHost? _host;
	private TrayService? _trayService;

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		_host = Host.CreateDefaultBuilder()
			.ConfigureServices(services =>
			{
				services.AddLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddDebug();
					builder.AddProvider(new LocalFileLoggerProvider());
				});

				services.AddDownloadPilotInfrastructure();
				services.AddSingleton<ProposalNotificationService>();
				services.AddSingleton<INotificationService>(provider =>
					provider.GetRequiredService<ProposalNotificationService>());
				services.AddSingleton<ManualScanService>();
				services.AddSingleton<MainViewModel>();
				services.AddSingleton<MainWindow>();
				services.AddSingleton<TrayService>();
			})
			.Build();

		await _host.StartAsync();

		var window = _host.Services.GetRequiredService<MainWindow>();
		MainWindow = window;
		window.Show();

		var vm = _host.Services.GetRequiredService<MainViewModel>();
		await vm.InitializeAsync();

		var weeklyReportDirectory = GetWeeklyReportDirectory(e.Args);
		if (weeklyReportDirectory is not null)
		{
			await vm.ExportWeeklyCleanupReportToDirectoryAsync(weeklyReportDirectory, CancellationToken.None);
			window.AllowClose();
			Shutdown();
			return;
		}

		var captureDirectory = GetCaptureDirectory(e.Args);
		if (captureDirectory is not null)
		{
			await Dispatcher.InvokeAsync(
				() => CapturePreviews(window, captureDirectory),
				DispatcherPriority.ApplicationIdle);
			window.AllowClose();
			Shutdown();
			return;
		}

		_ = vm.CheckForUpdatesOnStartupAsync();
		_trayService = _host.Services.GetRequiredService<TrayService>();
	}

	private static string? GetCaptureDirectory(IReadOnlyList<string> args)
	{
		for (var index = 0; index < args.Count - 1; index++)
		{
			if (args[index].Equals("--capture-preview", StringComparison.OrdinalIgnoreCase))
			{
				return Path.GetFullPath(args[index + 1]);
			}
		}

		return null;
	}

	private static string? GetWeeklyReportDirectory(IReadOnlyList<string> args)
	{
		for (var index = 0; index < args.Count - 1; index++)
		{
			if (args[index].Equals("--weekly-report", StringComparison.OrdinalIgnoreCase))
			{
				return Path.GetFullPath(args[index + 1]);
			}
		}

		return null;
	}

	private static void CapturePreviews(MainWindow window, string outputDirectory)
	{
		Directory.CreateDirectory(outputDirectory);
		window.ShowInTaskbar = false;
		window.OnboardingOverlay.Visibility = Visibility.Visible;
		window.UpdateLayout();
		CaptureWindow(window, Path.Combine(outputDirectory, "downloadpilot-wizard.png"));

		window.OnboardingOverlay.Visibility = Visibility.Collapsed;
		window.UpdateLayout();
		CaptureWindow(window, Path.Combine(outputDirectory, "downloadpilot-dashboard.png"));

		CapturePage(window, selectedIndex: 1, Path.Combine(outputDirectory, "downloadpilot-smart-inbox.png"));
		CapturePage(window, selectedIndex: 2, Path.Combine(outputDirectory, "downloadpilot-smart-tools.png"));
		CapturePage(window, selectedIndex: 3, Path.Combine(outputDirectory, "downloadpilot-mailfilter.png"));
		CapturePage(window, selectedIndex: 4, Path.Combine(outputDirectory, "downloadpilot-new-files.png"));
		CapturePage(window, selectedIndex: 5, Path.Combine(outputDirectory, "downloadpilot-rules.png"));
		CapturePage(window, selectedIndex: 8, Path.Combine(outputDirectory, "downloadpilot-duplicates.png"));
	}

	private static void CapturePage(MainWindow window, int selectedIndex, string outputPath)
	{
		window.NavigationList.SelectedIndex = selectedIndex;
		window.ContentTabs.SelectedIndex = selectedIndex;
		window.UpdateLayout();
		window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
		window.UpdateLayout();
		CaptureWindow(window, outputPath);
	}

	private static void CaptureWindow(Window window, string outputPath)
	{
		var dpi = VisualTreeHelper.GetDpi(window);
		var width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX));
		var height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY));
		var bitmap = new RenderTargetBitmap(
			width,
			height,
			96d * dpi.DpiScaleX,
			96d * dpi.DpiScaleY,
			PixelFormats.Pbgra32);

		bitmap.Render(window);
		var encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using var stream = File.Create(outputPath);
		encoder.Save(stream);
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		_trayService?.Dispose();

		if (_host is not null)
		{
			await _host.StopAsync();
			_host.Dispose();
		}

		base.OnExit(e);
	}
}


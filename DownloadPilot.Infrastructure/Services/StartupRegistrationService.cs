using DownloadPilot.Core.Abstractions;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace DownloadPilot.Infrastructure.Services;

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DownloadPilot";

    public bool IsSupported => OperatingSystem.IsWindows();

    public Task SetEnabledAsync(bool enabled, string executablePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(executablePath))
        {
            return Task.CompletedTask;
        }

        SetEnabledOnWindows(enabled, executablePath);

        return Task.CompletedTask;
    }

    [SupportedOSPlatform("windows")]
    private static void SetEnabledOnWindows(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(AppName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}

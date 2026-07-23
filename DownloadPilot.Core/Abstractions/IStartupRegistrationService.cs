namespace DownloadPilot.Core.Abstractions;

public interface IStartupRegistrationService
{
    bool IsSupported { get; }

    Task SetEnabledAsync(bool enabled, string executablePath, CancellationToken cancellationToken);
}

namespace DownloadPilot.Core.Models;

public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes, string Status)
{
    public bool HasKnownTotal => TotalBytes is > 0;

    public double? Percentage => HasKnownTotal
        ? Math.Clamp(BytesReceived * 100d / TotalBytes!.Value, 0d, 100d)
        : null;
}

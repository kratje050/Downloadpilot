using System.Security.Cryptography;
using DownloadPilot.Core.Abstractions;

namespace DownloadPilot.Infrastructure.Services;

public sealed class FileHashService : IFileHashService
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}

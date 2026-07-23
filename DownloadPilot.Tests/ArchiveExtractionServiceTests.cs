using System.IO.Compression;
using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class ArchiveExtractionServiceTests
{
    [Fact]
    public async Task ExtractZipSafelyAsync_ShouldBlockPathTraversal()
    {
        var service = new ArchiveExtractionService();
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var zipPath = Path.Combine(root, "bad.zip");
        var extractPath = Path.Combine(root, "out");

        try
        {
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("..\\evil.txt");
                await using var stream = entry.Open();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync("x");
            }

            await Assert.ThrowsAsync<InvalidDataException>(
                () => service.ExtractZipSafelyAsync(zipPath, extractPath, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

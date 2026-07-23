using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class FileHashServiceTests
{
    [Fact]
    public async Task ComputeSha256Async_ShouldReturnStableHash()
    {
        var service = new FileHashService();
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            await File.WriteAllTextAsync(tempFile, "DownloadPilot");
            var hash1 = await service.ComputeSha256Async(tempFile, CancellationToken.None);
            var hash2 = await service.ComputeSha256Async(tempFile, CancellationToken.None);

            Assert.Equal(hash1, hash2);
            Assert.NotEmpty(hash1);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

using DownloadPilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DownloadPilot.Tests;

public sealed class FileStabilityServiceTests
{
    [Fact]
    public async Task WaitUntilStableAsync_ShouldIgnoreTemporaryDownloadExtensions()
    {
        var service = new FileStabilityService(NullLogger<FileStabilityService>.Instance);
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".crdownload");

        try
        {
            await File.WriteAllTextAsync(tempFile, "incomplete");

            var result = await service.WaitUntilStableAsync(tempFile, CancellationToken.None);

            Assert.False(result);
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

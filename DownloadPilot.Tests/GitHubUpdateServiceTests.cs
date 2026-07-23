using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class GitHubUpdateServiceTests
{
    [Theory]
    [InlineData("v0.2.0", "0.1.0", true)]
    [InlineData("0.1.0", "0.1.0", false)]
    [InlineData("v0.1.0-beta", "0.1.0", false)]
    [InlineData("1.0.0", "0.9.9", true)]
    [InlineData(null, "0.1.0", false)]
    public void IsNewerVersion_ShouldCompareReleaseTags(string? latest, string current, bool expected)
    {
        Assert.Equal(expected, GitHubUpdateService.IsNewerVersion(latest, current));
    }

    [Fact]
    public void StartDownloadedUpdate_ShouldReturnFalseForMissingAsset()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.zip");

        Assert.False(GitHubUpdateService.StartDownloadedUpdate(missingPath));
    }
}

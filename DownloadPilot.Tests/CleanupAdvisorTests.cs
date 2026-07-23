using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class CleanupAdvisorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DownloadPilotCleanupAdvisorTest-" + Guid.NewGuid());

    [Fact]
    public void ScanDriverDownloads_ShouldFindOldDriverInstaller()
    {
        var downloads = Path.Combine(_root, "Downloads");
        Directory.CreateDirectory(downloads);
        var installer = Path.Combine(downloads, "NVIDIA-driver-setup.exe");
        File.WriteAllBytes(installer, new byte[2048]);
        File.SetLastWriteTime(installer, DateTime.Now.AddDays(-45));

        var results = CleanupAdvisor.ScanDriverDownloads([downloads]);

        Assert.Contains(results, result => result.Title == "NVIDIA-driver-setup.exe");
    }

    [Fact]
    public void ScanAppResidues_ShouldFlagUnknownAppDataFolder()
    {
        var appData = Path.Combine(_root, "AppData", "Local");
        var residue = Path.Combine(appData, "DefinitelyRemovedExampleApp");
        Directory.CreateDirectory(residue);
        File.WriteAllBytes(Path.Combine(residue, "cache.bin"), new byte[11 * 1024 * 1024]);
        Directory.SetLastWriteTime(residue, DateTime.Now.AddDays(-90));

        var results = CleanupAdvisor.ScanAppResidues([appData]);

        Assert.Contains(results, result =>
            result.Title == "DefinitelyRemovedExampleApp"
            && result.Category == "Mogelijk restant");
    }

    [Fact]
    public void BuildStorageMap_ShouldOrderFoldersBySize()
    {
        var small = Path.Combine(_root, "Small");
        var large = Path.Combine(_root, "Large");
        Directory.CreateDirectory(small);
        Directory.CreateDirectory(large);
        File.WriteAllBytes(Path.Combine(small, "small.bin"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(large, "large.bin"), new byte[4096]);

        var results = CleanupAdvisor.BuildStorageMap([small, large]);

        Assert.Equal("Large", results.First().Title);
    }

    [Fact]
    public void RunPowerAudit_ShouldFindPrivacyCloudAndInstallerSignals()
    {
        var downloads = Path.Combine(_root, "Downloads");
        Directory.CreateDirectory(downloads);
        File.WriteAllText(Path.Combine(downloads, "belasting-wachtwoord.txt"), "mijn bsn staat hier");
        File.WriteAllText(Path.Combine(downloads, "rapport conflicted copy.docx"), "copy");
        File.WriteAllBytes(Path.Combine(downloads, "ExampleApp-1.0.0-setup.exe"), new byte[512]);
        File.WriteAllBytes(Path.Combine(downloads, "ExampleApp-2.0.0-setup.exe"), new byte[1024]);

        var results = CleanupAdvisor.RunPowerAudit([downloads]);

        Assert.Contains(results, result => result.Category == "Privacy");
        Assert.Contains(results, result => result.Category == "Cloud-conflict");
        Assert.Contains(results, result => result.Category == "Installer-versies");
        Assert.Contains(results, result => result.Category == "Healthscore");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

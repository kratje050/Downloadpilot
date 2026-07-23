using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class GameResidueScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DownloadPilotGameResidueTest-" + Guid.NewGuid());

    [Fact]
    public void Scan_ShouldIgnoreAppDataFolderMatchingEpicInstalledGame()
    {
        var epicManifestRoot = Path.Combine(_root, "Epic", "Manifests");
        Directory.CreateDirectory(epicManifestRoot);
        File.WriteAllText(
            Path.Combine(epicManifestRoot, "game.item"),
            """
            {
                "DisplayName": "Rocket Example",
                "AppName": "RocketExample",
                "InstallLocation": "D:\\Games\\RocketExample"
            }
            """);

        var appData = Path.Combine(_root, "AppData", "Local");
        var installedResidue = Path.Combine(appData, "Rocket Example", "Saved", "SaveGames");
        Directory.CreateDirectory(installedResidue);
        File.WriteAllText(Path.Combine(installedResidue, "slot1.sav"), "save");

        var results = GameResidueScanner.Scan(new GameResidueScanOptions
        {
            EpicManifestRoots = [epicManifestRoot],
            ScanRoots = [appData]
        });

        Assert.DoesNotContain(results, result => result.Name == "Rocket Example");
    }

    [Fact]
    public void Scan_ShouldFindRemovedModProfileFolder()
    {
        var appData = Path.Combine(_root, "AppData", "Roaming");
        var modProfile = Path.Combine(appData, "Vortex", "Forgotten Game", "mods");
        Directory.CreateDirectory(modProfile);
        File.WriteAllText(Path.Combine(modProfile, "example-mod.esp"), "mod");
        Directory.SetLastWriteTime(Path.Combine(appData, "Vortex", "Forgotten Game"), DateTime.Now.AddDays(-90));

        var results = GameResidueScanner.Scan(new GameResidueScanOptions
        {
            ScanRoots = [Path.Combine(appData, "Vortex")]
        });

        Assert.Contains(results, result => result.Name == "Forgotten Game");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

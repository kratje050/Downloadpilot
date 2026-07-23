using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class SteamResidueScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DownloadPilotSteamTest-" + Guid.NewGuid());

    [Fact]
    public void Scan_ShouldFindGameLikeAppDataFolderThatIsNotInstalled()
    {
        var steamRoot = CreateSteamRoot(("Installed Game", "InstalledGame"));
        var appData = Path.Combine(_root, "AppData", "Local");
        var residue = Path.Combine(appData, "Removed Game", "Saved", "SaveGames");
        Directory.CreateDirectory(residue);
        File.WriteAllText(Path.Combine(residue, "slot1.sav"), "save");
        File.WriteAllText(Path.Combine(appData, "Removed Game", "steam_autocloud.vdf"), "cloud");

        var results = SteamResidueScanner.Scan(new SteamResidueScanOptions
        {
            SteamRoots = [steamRoot],
            AppDataRoots = [appData]
        });

        Assert.Contains(results, result => result.Name == "Removed Game");
    }

    [Fact]
    public void Scan_ShouldIgnoreFoldersMatchingInstalledSteamGame()
    {
        var steamRoot = CreateSteamRoot(("Installed Game", "InstalledGame"));
        var appData = Path.Combine(_root, "AppData", "Local");
        var installedFolder = Path.Combine(appData, "Installed Game", "Saved");
        Directory.CreateDirectory(installedFolder);
        File.WriteAllText(Path.Combine(installedFolder, "slot1.sav"), "save");
        File.WriteAllText(Path.Combine(appData, "Installed Game", "steam_autocloud.vdf"), "cloud");

        var results = SteamResidueScanner.Scan(new SteamResidueScanOptions
        {
            SteamRoots = [steamRoot],
            AppDataRoots = [appData]
        });

        Assert.DoesNotContain(results, result => result.Name == "Installed Game");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateSteamRoot((string Name, string InstallDir) game)
    {
        var steamRoot = Path.Combine(_root, "Steam");
        var steamApps = Path.Combine(steamRoot, "steamapps");
        Directory.CreateDirectory(steamApps);
        File.WriteAllText(
            Path.Combine(steamApps, "appmanifest_10.acf"),
            "\"AppState\"\n{\n" +
            "    \"appid\" \"10\"\n" +
            $"    \"name\" \"{game.Name}\"\n" +
            $"    \"installdir\" \"{game.InstallDir}\"\n" +
            "}\n");
        return steamRoot;
    }
}

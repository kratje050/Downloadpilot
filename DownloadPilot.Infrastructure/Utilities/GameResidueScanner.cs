using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DownloadPilot.Infrastructure.Utilities;

public static partial class GameResidueScanner
{
    private static readonly string[] SaveFileExtensions =
    [
        ".sav",
        ".save",
        ".slot",
        ".profile",
        ".arkprofile",
        ".ess",
        ".fos",
        ".dat"
    ];

    private static readonly string[] ModFileExtensions =
    [
        ".pak",
        ".esp",
        ".esm",
        ".bsa",
        ".mod",
        ".dll"
    ];

    private static readonly string[] SaveDirectoryHints =
    [
        "saved",
        "save",
        "saves",
        "savegames",
        "config",
        "settings",
        "profiles",
        "steam_autocloud",
        "crashes",
        "logs"
    ];

    private static readonly string[] ModDirectoryHints =
    [
        "mods",
        "mod",
        "modprofiles",
        "mod organizer",
        "plugins",
        "overrides",
        "workshop",
        "vortex",
        "curseforge",
        "thunderstore",
        "r2modman",
        "modrinth",
        "prism",
        "multimc",
        "gdlauncher",
        "atlauncher",
        "overwolf"
    ];

    private static readonly HashSet<string> SkippedTopLevelFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Adobe",
        "AMD",
        "Apple",
        "Comms",
        "ConnectedDevicesPlatform",
        "CrashDumps",
        "Discord",
        "Google",
        "Microsoft",
        "MicrosoftEdge",
        "Mozilla",
        "NVIDIA",
        "Packages",
        "Programs",
        "Publishers",
        "Temp",
        "WindowsApps"
    };

    private static readonly string[] LauncherFolderHints =
    [
        "Steam",
        "Epic",
        "EpicGamesLauncher",
        "GOG",
        "GOG.com",
        "Galaxy",
        "Electronic Arts",
        "EA Desktop",
        "Origin",
        "Ubisoft",
        "Ubisoft Game Launcher",
        "Battle.net",
        "Blizzard",
        "Xbox",
        "Rockstar Games",
        "Rockstar",
        "Bethesda.net Launcher",
        "Bethesda",
        "Riot Client",
        "Minecraft",
        ".minecraft",
        "CurseForge",
        "Vortex",
        "NexusMods",
        "ModOrganizer",
        "Mod Organizer 2",
        "Thunderstore Mod Manager",
        "r2modmanPlus-local",
        "PrismLauncher",
        "MultiMC",
        "ModrinthApp",
        "GDLauncher",
        "ATLauncher",
        "Overwolf",
        "itch",
        "itch.io",
        "Heroic",
        "Legendary",
        "Playnite"
    ];

    public static IReadOnlyList<GameResidueCandidate> Scan(GameResidueScanOptions? options = null)
    {
        options ??= new GameResidueScanOptions();
        var installedGameKeys = LoadInstalledGameKeys(options);
        var scanRoots = ResolveScanRoots(options.ScanRoots);
        var candidates = new List<GameResidueCandidate>();

        foreach (var root in scanRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var folder in EnumerateCandidateFolders(root))
            {
                var candidate = AnalyzeFolder(root, folder, installedGameKeys);
                if (candidate is not null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates
            .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.Confidence).First())
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenByDescending(candidate => candidate.SizeBytes)
            .Take(Math.Clamp(options.MaxCandidates, 1, 1000))
            .ToList();
    }

    private static GameResidueCandidate? AnalyzeFolder(
        string root,
        string folder,
        HashSet<string> installedGameKeys)
    {
        var folderName = Path.GetFileName(folder);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return null;
        }

        if (LooksLikeInstalledGameFolder(folder, installedGameKeys))
        {
            return null;
        }

        var signals = CollectSignals(folder);
        var score = installedGameKeys.Count > 0 ? 12 : 5;
        var reasons = new List<string>();

        if (installedGameKeys.Count > 0)
        {
            reasons.Add("geen match met huidige launcher-installaties");
        }

        if (signals.HasLauncherFile)
        {
            score += 35;
            reasons.Add("launcher/config spoor gevonden");
        }

        if (signals.HasSaveLikeFile)
        {
            score += 24;
            reasons.Add("save-achtig bestand gevonden");
        }

        if (signals.HasSaveLikeDirectory)
        {
            score += 18;
            reasons.Add("save/config map gevonden");
        }

        if (signals.HasGameEngineHint)
        {
            score += 16;
            reasons.Add("Unity/Unreal game-spoor gevonden");
        }

        if (signals.HasModLikeFile)
        {
            score += 22;
            reasons.Add("mod-bestand gevonden");
        }

        if (signals.HasModLikeDirectory)
        {
            score += 18;
            reasons.Add("mod/profiel map gevonden");
        }

        var pathSource = DetectSource(root, folder);
        if (pathSource.Contains("Mods", StringComparison.OrdinalIgnoreCase)
            || pathSource.Contains("manager", StringComparison.OrdinalIgnoreCase))
        {
            score += 12;
            reasons.Add("bekende modmanager-locatie");
        }

        var lastWrite = SafeLastWrite(folder);
        var ageDays = (DateTime.Now - lastWrite).TotalDays;
        if (ageDays >= 180)
        {
            score += 16;
            reasons.Add("ouder dan 180 dagen");
        }
        else if (ageDays >= 45)
        {
            score += 9;
            reasons.Add("ouder dan 45 dagen");
        }

        var size = SafeDirectorySize(folder);
        if (size >= 100L * 1024L * 1024L)
        {
            score += 10;
            reasons.Add("neemt veel ruimte in");
        }

        score = Math.Clamp(score, 0, 100);
        if (score < 45)
        {
            return null;
        }

        var rootName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return new GameResidueCandidate
        {
            Path = folder,
            Name = folderName,
            RootName = string.IsNullOrWhiteSpace(rootName) ? root : rootName,
            Source = pathSource,
            SizeBytes = size,
            LastWriteLocal = lastWrite,
            Confidence = score,
            Reason = string.Join("; ", reasons.Distinct(StringComparer.OrdinalIgnoreCase))
        };
    }

    private static IEnumerable<string> EnumerateCandidateFolders(string root)
    {
        var rootName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var isSpecialGameRoot = IsKnownGameOrModRoot(rootName) || root.Contains("My Games", StringComparison.OrdinalIgnoreCase);

        foreach (var topLevel in SafeEnumerateDirectories(root))
        {
            var topName = Path.GetFileName(topLevel);
            if (!isSpecialGameRoot && SkippedTopLevelFolders.Contains(topName))
            {
                continue;
            }

            yield return topLevel;

            var childLimit = isSpecialGameRoot ? 150 : 75;
            foreach (var child in SafeEnumerateDirectories(topLevel).Take(childLimit))
            {
                yield return child;
            }
        }
    }

    private static GameFolderSignals CollectSignals(string folder)
    {
        var signals = new GameFolderSignals();
        var inspectedFiles = 0;
        var inspectedDirectories = 0;

        foreach (var directory in SafeEnumerateDirectories(folder, SearchOption.AllDirectories))
        {
            inspectedDirectories++;
            var name = Path.GetFileName(directory);
            if (SaveDirectoryHints.Any(hint => name.Contains(hint, StringComparison.OrdinalIgnoreCase)))
            {
                signals.HasSaveLikeDirectory = true;
            }

            if (ModDirectoryHints.Any(hint => name.Contains(hint, StringComparison.OrdinalIgnoreCase)))
            {
                signals.HasModLikeDirectory = true;
            }

            if (name.Contains("unity", StringComparison.OrdinalIgnoreCase)
                || name.Contains("unreal", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasGameEngineHint = true;
            }

            if (inspectedDirectories >= 350)
            {
                break;
            }
        }

        foreach (var file in SafeEnumerateFiles(folder, SearchOption.AllDirectories))
        {
            inspectedFiles++;
            var fileName = Path.GetFileName(file);
            var extension = Path.GetExtension(file);

            if (fileName.Equals("steam_autocloud.vdf", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("steam_appid.txt", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("GameUserSettings.ini", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("modlist.txt", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("plugins.txt", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasLauncherFile = true;
            }

            if (SaveFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                || fileName.Contains("Player.log", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasSaveLikeFile = true;
            }

            if (ModFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                || fileName.Contains("mods.yml", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("loadorder", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasModLikeFile = true;
            }

            if (fileName.Contains("Player.log", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains("CrashContext.runtime-xml", StringComparison.OrdinalIgnoreCase))
            {
                signals.HasGameEngineHint = true;
            }

            if (inspectedFiles >= 1200)
            {
                break;
            }
        }

        return signals;
    }

    private static HashSet<string> LoadInstalledGameKeys(GameResidueScanOptions options)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LoadSteamInstalledGameKeys(keys, options.SteamRoots);
        LoadEpicInstalledGameKeys(keys, options.EpicManifestRoots);
        LoadInstalledGameRootKeys(keys, options.InstalledGameRoots);
        LoadRegistryInstalledGameKeys(keys);
        return keys;
    }

    private static void LoadSteamInstalledGameKeys(HashSet<string> keys, IReadOnlyList<string>? configuredSteamRoots)
    {
        foreach (var library in ResolveSteamLibraries(configuredSteamRoots))
        {
            var steamApps = Path.Combine(library, "steamapps");
            foreach (var manifest in SafeEnumerateFiles(steamApps, "appmanifest_*.acf"))
            {
                var text = SafeReadAllText(manifest);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                AddGameKey(keys, ReadVdfValue(text, "name"));
                AddGameKey(keys, ReadVdfValue(text, "installdir"));
            }
        }
    }

    private static void LoadEpicInstalledGameKeys(HashSet<string> keys, IReadOnlyList<string>? configuredManifestRoots)
    {
        foreach (var root in ResolveEpicManifestRoots(configuredManifestRoots))
        {
            foreach (var manifest in SafeEnumerateFiles(root, "*.item"))
            {
                var text = SafeReadAllText(manifest);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                AddJsonOrRegexValue(keys, text, "DisplayName");
                AddJsonOrRegexValue(keys, text, "AppName");
                AddGameKey(keys, Path.GetFileName(ReadJsonOrRegexValue(text, "InstallLocation")));
            }
        }
    }

    private static void LoadInstalledGameRootKeys(HashSet<string> keys, IReadOnlyList<string>? configuredRoots)
    {
        foreach (var root in ResolveInstalledGameRoots(configuredRoots))
        {
            foreach (var directory in SafeEnumerateDirectories(root))
            {
                AddGameKey(keys, Path.GetFileName(directory));
            }
        }
    }

    private static void LoadRegistryInstalledGameKeys(HashSet<string> keys)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            LoadRegistryInstalledGameKeys(keys, hive, uninstallPath);
            LoadRegistryInstalledGameKeys(keys, hive, $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void LoadRegistryInstalledGameKeys(HashSet<string> keys, RegistryKey hive, string uninstallPath)
    {
        try
        {
            using var uninstallKey = hive.OpenSubKey(uninstallPath);
            if (uninstallKey is null)
            {
                return;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var appKey = uninstallKey.OpenSubKey(subKeyName);
                if (appKey is null)
                {
                    continue;
                }

                var displayName = appKey.GetValue("DisplayName") as string;
                var publisher = appKey.GetValue("Publisher") as string;
                var installLocation = appKey.GetValue("InstallLocation") as string;
                var installSource = appKey.GetValue("InstallSource") as string;
                if (!LooksLikeGameLauncherEntry(displayName, publisher, installLocation, installSource))
                {
                    continue;
                }

                AddGameKey(keys, displayName);
                AddGameKey(keys, Path.GetFileName(installLocation));
            }
        }
        catch
        {
            // Registry access can be blocked for some entries; scanning should stay best-effort.
        }
    }

    private static bool LooksLikeGameLauncherEntry(params string?[] values)
    {
        var combined = string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        return LauncherFolderHints.Any(hint => combined.Contains(hint, StringComparison.OrdinalIgnoreCase))
            || combined.Contains("game", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("launcher", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ResolveSteamLibraries(IReadOnlyList<string>? configuredSteamRoots)
    {
        var roots = configuredSteamRoots is { Count: > 0 }
            ? configuredSteamRoots
            : DefaultSteamRoots();
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots.Where(Directory.Exists))
        {
            libraries.Add(root);
            var libraryFile = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            var text = SafeReadAllText(libraryFile);
            foreach (Match match in SteamPathRegex().Matches(text))
            {
                var path = match.Groups["path"].Value.Replace(@"\\", @"\", StringComparison.Ordinal);
                if (Directory.Exists(path))
                {
                    libraries.Add(path);
                }
            }
        }

        return libraries;
    }

    private static IReadOnlyList<string> DefaultSteamRoots()
    {
        var roots = new List<string>();
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        AddIfPresent(roots, Path.Combine(programFilesX86, "Steam"));
        AddIfPresent(roots, Path.Combine(programFiles, "Steam"));
        return roots;
    }

    private static IReadOnlyList<string> ResolveEpicManifestRoots(IReadOnlyList<string>? configuredRoots)
    {
        if (configuredRoots is { Count: > 0 })
        {
            return configuredRoots;
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return
        [
            Path.Combine(programData, "Epic", "EpicGamesLauncher", "Data", "Manifests")
        ];
    }

    private static IReadOnlyList<string> ResolveInstalledGameRoots(IReadOnlyList<string>? configuredRoots)
    {
        if (configuredRoots is { Count: > 0 })
        {
            return configuredRoots;
        }

        var roots = new List<string>();
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        foreach (var basePath in new[] { programFiles, programFilesX86 }.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            AddIfPresent(roots, Path.Combine(basePath, "Epic Games"));
            AddIfPresent(roots, Path.Combine(basePath, "GOG Galaxy", "Games"));
            AddIfPresent(roots, Path.Combine(basePath, "GOG Games"));
            AddIfPresent(roots, Path.Combine(basePath, "EA Games"));
            AddIfPresent(roots, Path.Combine(basePath, "Electronic Arts"));
            AddIfPresent(roots, Path.Combine(basePath, "Ubisoft", "Ubisoft Game Launcher", "games"));
            AddIfPresent(roots, Path.Combine(basePath, "Battle.net"));
            AddIfPresent(roots, Path.Combine(basePath, "World of Warcraft"));
            AddIfPresent(roots, Path.Combine(basePath, "Diablo IV"));
            AddIfPresent(roots, Path.Combine(basePath, "Riot Games"));
            AddIfPresent(roots, Path.Combine(basePath, "Rockstar Games"));
            AddIfPresent(roots, Path.Combine(basePath, "Bethesda.net Launcher", "games"));
            AddIfPresent(roots, Path.Combine(basePath, "XboxGames"));
            AddIfPresent(roots, Path.Combine(basePath, "ModifiableWindowsApps"));
        }

        return roots;
    }

    private static IReadOnlyList<string> ResolveScanRoots(IReadOnlyList<string>? configuredRoots)
    {
        if (configuredRoots is { Count: > 0 })
        {
            return configuredRoots;
        }

        var roots = new List<string>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        AddIfPresent(roots, local);
        AddIfPresent(roots, roaming);
        AddIfPresent(roots, Path.Combine(userProfile, "AppData", "LocalLow"));
        AddIfPresent(roots, Path.Combine(documents, "My Games"));
        AddIfPresent(roots, Path.Combine(documents, "Rockstar Games"));
        AddIfPresent(roots, Path.Combine(userProfile, "Saved Games"));

        AddIfPresent(roots, Path.Combine(local, "Battle.net"));
        AddIfPresent(roots, Path.Combine(local, "Blizzard Entertainment"));
        AddIfPresent(roots, Path.Combine(local, "Electronic Arts"));
        AddIfPresent(roots, Path.Combine(local, "EpicGamesLauncher"));
        AddIfPresent(roots, Path.Combine(local, "GOG.com"));
        AddIfPresent(roots, Path.Combine(local, "Heroic"));
        AddIfPresent(roots, Path.Combine(local, "itch"));
        AddIfPresent(roots, Path.Combine(local, "Origin"));
        AddIfPresent(roots, Path.Combine(local, "Overwolf"));
        AddIfPresent(roots, Path.Combine(local, "Playnite"));
        AddIfPresent(roots, Path.Combine(local, "Riot Games"));
        AddIfPresent(roots, Path.Combine(local, "Rockstar Games"));
        AddIfPresent(roots, Path.Combine(local, "Ubisoft Game Launcher"));
        AddIfPresent(roots, Path.Combine(local, "Xbox"));
        AddIfPresent(roots, Path.Combine(roaming, ".minecraft"));
        AddIfPresent(roots, Path.Combine(roaming, "ATLauncher"));
        AddIfPresent(roots, Path.Combine(roaming, "Battle.net"));
        AddIfPresent(roots, Path.Combine(roaming, "Blizzard Entertainment"));
        AddIfPresent(roots, Path.Combine(roaming, "Vortex"));
        AddIfPresent(roots, Path.Combine(roaming, "CurseForge"));
        AddIfPresent(roots, Path.Combine(roaming, "GDLauncher"));
        AddIfPresent(roots, Path.Combine(roaming, "GOG.com"));
        AddIfPresent(roots, Path.Combine(roaming, "ModrinthApp"));
        AddIfPresent(roots, Path.Combine(roaming, "MultiMC"));
        AddIfPresent(roots, Path.Combine(roaming, "Overwolf"));
        AddIfPresent(roots, Path.Combine(roaming, "PrismLauncher"));
        AddIfPresent(roots, Path.Combine(roaming, "r2modmanPlus-local"));
        AddIfPresent(roots, Path.Combine(roaming, "Thunderstore Mod Manager"));
        AddIfPresent(roots, Path.Combine(local, "ModOrganizer"));
        AddIfPresent(roots, Path.Combine(local, "NexusMods"));
        AddIfPresent(roots, Path.Combine(programData, "Vortex"));
        AddIfPresent(roots, Path.Combine(programData, "ModOrganizer"));

        return roots
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool LooksLikeInstalledGameFolder(string folder, HashSet<string> installedGameKeys)
    {
        if (installedGameKeys.Count == 0)
        {
            return false;
        }

        var folderName = NormalizeKey(Path.GetFileName(folder));
        var parentName = NormalizeKey(Path.GetFileName(Path.GetDirectoryName(folder) ?? string.Empty));
        return installedGameKeys.Contains(folderName)
            || installedGameKeys.Contains(parentName)
            || installedGameKeys.Any(key =>
                key.Length >= 5
                && folderName.Length >= 5
                && (folderName.Contains(key, StringComparison.OrdinalIgnoreCase)
                    || key.Contains(folderName, StringComparison.OrdinalIgnoreCase)));
    }

    private static string DetectSource(string root, string folder)
    {
        var combined = $"{root} {folder}";
        foreach (var hint in LauncherFolderHints)
        {
            if (combined.Contains(hint, StringComparison.OrdinalIgnoreCase))
            {
                return hint;
            }
        }

        if (combined.Contains("My Games", StringComparison.OrdinalIgnoreCase))
        {
            return "Documents\\My Games";
        }

        if (combined.Contains("Saved Games", StringComparison.OrdinalIgnoreCase))
        {
            return "Saved Games";
        }

        var rootName = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(rootName) ? root : rootName;
    }

    private static bool IsKnownGameOrModRoot(string rootName)
    {
        return LauncherFolderHints.Any(hint => rootName.Contains(hint, StringComparison.OrdinalIgnoreCase))
            || rootName.Contains("My Games", StringComparison.OrdinalIgnoreCase)
            || rootName.Contains("Saved Games", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddJsonOrRegexValue(HashSet<string> keys, string text, string propertyName)
    {
        AddGameKey(keys, ReadJsonOrRegexValue(text, propertyName));
    }

    private static string? ReadJsonOrRegexValue(string text, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }
        catch (JsonException)
        {
            // Some launcher manifests are JSON-like enough for regex fallback but not strict JSON.
        }

        var match = Regex.Match(
            text,
            $"\"{Regex.Escape(propertyName)}\"\\s*:\\s*\"(?<value>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static void AddGameKey(HashSet<string> keys, string? value)
    {
        var key = NormalizeKey(value);
        if (!string.IsNullOrWhiteSpace(key))
        {
            keys.Add(key);
        }
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var matches = AlphaNumericRegex().Matches(value.ToLowerInvariant());
        return string.Concat(matches.Select(match => match.Value));
    }

    private static string? ReadVdfValue(string text, string key)
    {
        var match = Regex.Match(
            text,
            $"\"{Regex.Escape(key)}\"\\s+\"(?<value>[^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value : null;
    }

    private static void AddIfPresent(List<string> values, string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            values.Add(path);
        }
    }

    private static string SafeReadAllText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path, SearchOption option = SearchOption.TopDirectoryOnly)
    {
        if (option == SearchOption.AllDirectories)
        {
            return SafeEnumerateDirectoriesRecursive(path);
        }

        try
        {
            return Directory.Exists(path)
                ? Directory.EnumerateDirectories(path, "*", option).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path, SearchOption option = SearchOption.TopDirectoryOnly)
    {
        if (option == SearchOption.AllDirectories)
        {
            return SafeEnumerateFilesRecursive(path);
        }

        try
        {
            return Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", option).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectoriesRecursive(string path)
    {
        var pending = new Stack<string>();
        if (Directory.Exists(path))
        {
            pending.Push(path);
        }

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var child in SafeEnumerateDirectories(current))
            {
                yield return child;
                pending.Push(child);
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFilesRecursive(string path)
    {
        var pending = new Stack<string>();
        if (Directory.Exists(path))
        {
            pending.Push(path);
        }

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var file in SafeEnumerateFiles(current))
            {
                yield return file;
            }

            foreach (var child in SafeEnumerateDirectories(current))
            {
                pending.Push(child);
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern)
    {
        try
        {
            return Directory.Exists(path)
                ? Directory.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static long SafeDirectorySize(string folder)
    {
        long total = 0;
        var inspected = 0;
        foreach (var file in SafeEnumerateFiles(folder, SearchOption.AllDirectories))
        {
            try
            {
                total += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore files that disappear or are locked during scanning.
            }

            inspected++;
            if (inspected >= 6000)
            {
                break;
            }
        }

        return total;
    }

    private static DateTime SafeLastWrite(string folder)
    {
        try
        {
            return Directory.GetLastWriteTime(folder);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    [GeneratedRegex("\"path\"\\s+\"(?<path>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SteamPathRegex();

    [GeneratedRegex("[a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AlphaNumericRegex();

    private sealed class GameFolderSignals
    {
        public bool HasLauncherFile { get; set; }

        public bool HasSaveLikeFile { get; set; }

        public bool HasSaveLikeDirectory { get; set; }

        public bool HasGameEngineHint { get; set; }

        public bool HasModLikeFile { get; set; }

        public bool HasModLikeDirectory { get; set; }
    }
}

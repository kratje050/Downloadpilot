using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace DownloadPilot.Infrastructure.Utilities;

public static class CleanupAdvisor
{
    private static readonly string[] DriverNameHints =
    [
        "nvidia",
        "geforce",
        "amd",
        "radeon",
        "adrenalin",
        "intel",
        "driver",
        "graphics",
        "chipset",
        "realtek"
    ];

    private static readonly string[] InstallerExtensions =
    [
        ".exe",
        ".msi",
        ".zip",
        ".7z"
    ];

    private static readonly string[] GenericAppDataFolders =
    [
        "Microsoft",
        "Windows",
        "Packages",
        "Temp",
        "CrashDumps",
        "ConnectedDevicesPlatform",
        "Comms",
        "Programs"
    ];

    public static IReadOnlyList<CleanupAdvisorCandidate> ScanDriverDownloads(
        IReadOnlyList<string>? roots = null,
        int maxCandidates = 80)
    {
        var candidates = new List<CleanupAdvisorCandidate>();
        foreach (var root in ResolveDriverScanRoots(roots))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var rootName = DisplayName(root);
            var rootSize = SafeDirectorySize(root, maxFiles: 5000);
            if (IsKnownDriverCacheRoot(root) && rootSize > 0)
            {
                candidates.Add(new CleanupAdvisorCandidate
                {
                    Title = rootName,
                    Detail = "Bekende driver-cachemap. Controleer of er geen installatie bezig is.",
                    Metric = FormatBytes(rootSize),
                    Category = "Driver-cache",
                    Path = root,
                    Action = "Handmatig controleren",
                    SizeBytes = rootSize,
                    SafetyScore = 65
                });
            }

            foreach (var file in SafeEnumerateFiles(root, SearchOption.AllDirectories, maxItems: 5000))
            {
                var fileName = Path.GetFileName(file);
                var extension = Path.GetExtension(file);
                if (!InstallerExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                    || !DriverNameHints.Any(hint => fileName.Contains(hint, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var info = SafeFileInfo(file);
                if (info is null)
                {
                    continue;
                }

                var ageDays = (DateTime.Now - info.LastWriteTime).TotalDays;
                var safety = ageDays >= 30 ? 82 : 58;
                candidates.Add(new CleanupAdvisorCandidate
                {
                    Title = info.Name,
                    Detail = ageDays >= 30
                        ? $"Driver/installer lijkt oud: laatst gewijzigd {info.LastWriteTime:dd-MM-yyyy}"
                        : $"Recente driver/installer: laatst gewijzigd {info.LastWriteTime:dd-MM-yyyy}",
                    Metric = FormatBytes(info.Length),
                    Category = "Driver-download",
                    Path = info.FullName,
                    Action = ageDays >= 30 ? "Kandidaat voor Prullenbak" : "Eerst controleren",
                    SizeBytes = info.Length,
                    SafetyScore = safety
                });
            }
        }

        return candidates
            .GroupBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(candidate => candidate.SafetyScore)
            .ThenByDescending(candidate => candidate.SizeBytes)
            .Take(Math.Clamp(maxCandidates, 1, 250))
            .ToList();
    }

    public static IReadOnlyList<CleanupAdvisorCandidate> ScanAppResidues(
        IReadOnlyList<string>? roots = null,
        int maxCandidates = 80)
    {
        var installedKeys = LoadInstalledSoftwareKeys();
        var candidates = new List<CleanupAdvisorCandidate>();

        foreach (var root in ResolveAppDataRoots(roots))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var directory in SafeEnumerateDirectories(root, maxItems: 300))
            {
                var name = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(name)
                    || GenericAppDataFolders.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalized = Normalize(name);
                var isInstalled = installedKeys.Contains(normalized)
                    || installedKeys.Any(key =>
                        key.Length >= 5
                        && normalized.Length >= 5
                        && (key.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                            || normalized.Contains(key, StringComparison.OrdinalIgnoreCase)));

                var size = SafeDirectorySize(directory, maxFiles: 3000);
                var lastWrite = SafeLastWrite(directory);
                var ageDays = (DateTime.Now - lastWrite).TotalDays;
                if (size < 10L * 1024L * 1024L && ageDays < 60 && isInstalled)
                {
                    continue;
                }

                var likelyResidue = !isInstalled && (size >= 5L * 1024L * 1024L || ageDays >= 30);
                candidates.Add(new CleanupAdvisorCandidate
                {
                    Title = name,
                    Detail = isInstalled
                        ? $"Hoort waarschijnlijk bij een geïnstalleerde app. Laatst gewijzigd {lastWrite:dd-MM-yyyy}."
                        : $"Geen duidelijke match met geïnstalleerde apps. Laatst gewijzigd {lastWrite:dd-MM-yyyy}.",
                    Metric = FormatBytes(size),
                    Category = likelyResidue ? "Mogelijk restant" : "App-cache",
                    Path = directory,
                    Action = likelyResidue ? "Controleer na verwijderen app" : "Niet automatisch opruimen",
                    SizeBytes = size,
                    SafetyScore = likelyResidue ? 62 : 38
                });
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.SafetyScore)
            .ThenByDescending(candidate => candidate.SizeBytes)
            .Take(Math.Clamp(maxCandidates, 1, 250))
            .ToList();
    }

    public static IReadOnlyList<CleanupAdvisorCandidate> BuildStorageMap(
        IReadOnlyList<string>? roots = null,
        int maxCandidates = 12)
    {
        return ResolveStorageRoots(roots)
            .Where(Directory.Exists)
            .Select(root =>
            {
                var size = SafeDirectorySize(root, maxFiles: 10000);
                return new CleanupAdvisorCandidate
                {
                    Title = DisplayName(root),
                    Detail = $"Locatie: {root}",
                    Metric = FormatBytes(size),
                    Category = "Opslagkaart",
                    Path = root,
                    Action = "Grootste map eerst bekijken",
                    SizeBytes = size,
                    SafetyScore = 70
                };
            })
            .Where(candidate => candidate.SizeBytes > 0)
            .OrderByDescending(candidate => candidate.SizeBytes)
            .Take(Math.Clamp(maxCandidates, 1, 50))
            .ToList();
    }

    public static IReadOnlyList<CleanupAdvisorCandidate> RunPowerAudit(
        IReadOnlyList<string>? roots = null,
        int maxCandidates = 180)
    {
        var scanRoots = ResolvePowerAuditRoots(roots);
        var candidates = new List<CleanupAdvisorCandidate>();

        candidates.AddRange(ScanExternalBackupTargets());
        candidates.AddRange(ScanCloudConflicts(scanRoots));
        candidates.AddRange(ScanPrivacyFiles(scanRoots));
        candidates.AddRange(ScanMetadataCandidates(scanRoots));
        candidates.AddRange(ScanBrokenShortcuts(scanRoots));
        candidates.AddRange(ScanEmptyFolders(scanRoots));
        candidates.AddRange(ScanOldProjectFolders(scanRoots));
        candidates.AddRange(ScanSuspiciousDownloadSources(scanRoots));
        candidates.AddRange(ScanStartupItems());
        candidates.AddRange(ScanAppCacheLocations());
        candidates.AddRange(ScanLargeHiddenFiles(scanRoots));
        candidates.AddRange(ScanOldInstallerVersions(scanRoots));
        candidates.AddRange(ScanGenericNames(scanRoots));
        candidates.Add(BuildDownloadsHealthCandidate(scanRoots));
        candidates.Add(new CleanupAdvisorCandidate
        {
            Title = "Opruimkalender",
            Detail = "Weekrapportplanning bestaat al; plan daarnaast vaste maandelijkse game/restanten- en privacychecks.",
            Metric = "Planner",
            Category = "Opruimkalender",
            Action = "Gebruik weekrapport + maandrapport",
            SafetyScore = 90
        });
        candidates.Add(new CleanupAdvisorCandidate
        {
            Title = "Veilige testmap",
            Detail = "Gebruik Proefmodus en herstelpunten voordat je grote opruimacties uitvoert.",
            Metric = "Veilig",
            Category = "Testmap",
            Action = "Simuleer eerst",
            SafetyScore = 90
        });
        candidates.Add(new CleanupAdvisorCandidate
        {
            Title = "Duplicaatvergelijker",
            Detail = "Exacte duplicaten staan in de duplicaten-tab; vergelijk daar naam, map, datum, grootte en reden.",
            Metric = "Aanwezig",
            Category = "Duplicaten",
            Action = "Open tab Duplicaten",
            SafetyScore = 82
        });

        return candidates
            .GroupBy(candidate => $"{candidate.Category}|{candidate.Path}|{candidate.Title}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.SafetyScore).First())
            .OrderBy(candidate => candidate.Category, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(candidate => candidate.SizeBytes)
            .Take(Math.Clamp(maxCandidates, 1, 500))
            .ToList();
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanExternalBackupTargets()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch
        {
            yield break;
        }

        var removable = drives
            .Where(drive => drive.IsReady && drive.DriveType is DriveType.Removable or DriveType.Fixed)
            .Where(drive => drive.AvailableFreeSpace >= 5L * 1024L * 1024L * 1024L)
            .Where(drive => !drive.RootDirectory.FullName.StartsWith(
                Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\",
                StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (removable.Count == 0)
        {
            yield return new CleanupAdvisorCandidate
            {
                Title = "Geen externe backup-schijf gevonden",
                Detail = "Sluit een USB-schijf of externe SSD aan voor automatische backup-voorstellen.",
                Metric = "0 drives",
                Category = "Backup",
                Action = "Externe schijf aansluiten",
                SafetyScore = 70
            };
            yield break;
        }

        foreach (var drive in removable)
        {
            yield return new CleanupAdvisorCandidate
            {
                Title = drive.VolumeLabel.Length == 0 ? drive.Name : drive.VolumeLabel,
                Detail = $"Beschikbaar: {FormatBytes(drive.AvailableFreeSpace)} op {drive.Name}",
                Metric = FormatBytes(drive.AvailableFreeSpace),
                Category = "Backup",
                Path = drive.RootDirectory.FullName,
                Action = "Geschikt als backup-doel",
                SafetyScore = 88
            };
        }
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanCloudConflicts(IReadOnlyList<string> roots)
    {
        return EnumerateAuditFiles(roots)
            .Where(file =>
            {
                var name = Path.GetFileName(file);
                return name.Contains("conflicted copy", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("sync conflict", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("(1)", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("- Copy", StringComparison.OrdinalIgnoreCase);
            })
            .Take(40)
            .Select(file => BuildFileCandidate(file, "Cloud-conflict", "Mogelijke OneDrive/Google Drive conflict-kopie", "Vergelijk voor opruimen", 64));
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanPrivacyFiles(IReadOnlyList<string> roots)
    {
        string[] privacyHints =
        [
            "bsn",
            "wachtwoord",
            "password",
            "contract",
            "salaris",
            "loonstrook",
            "belasting",
            "paspoort",
            "rijbewijs",
            "bank",
            "iban",
            "medical",
            "medisch"
        ];

        return EnumerateAuditFiles(roots)
            .Where(file =>
            {
                var name = Path.GetFileName(file);
                return privacyHints.Any(hint => name.Contains(hint, StringComparison.OrdinalIgnoreCase))
                    || FileLooksSensitive(file, privacyHints);
            })
            .Take(50)
            .Select(file => BuildFileCandidate(file, "Privacy", "Mogelijk gevoelig bestand", "Bewaar bewust of verplaats naar veilige map", 45));
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanMetadataCandidates(IReadOnlyList<string> roots)
    {
        string[] imageExtensions = [".jpg", ".jpeg", ".tif", ".tiff", ".heic"];
        return EnumerateAuditFiles(roots)
            .Where(file => imageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Take(40)
            .Select(file => BuildFileCandidate(file, "EXIF/metadata", "Foto kan locatie/camera-metadata bevatten", "Metadata verwijderen voor delen", 72));
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanBrokenShortcuts(IReadOnlyList<string> roots)
    {
        foreach (var file in EnumerateAuditFiles(roots).Where(file => file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)).Take(120))
        {
            var target = TryReadShortcutTarget(file);
            if (string.IsNullOrWhiteSpace(target) || File.Exists(target) || Directory.Exists(target))
            {
                continue;
            }

            yield return BuildFileCandidate(file, "Snelkoppeling", $"Doel bestaat niet meer: {target}", "Kan meestal weg", 86);
        }
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanEmptyFolders(IReadOnlyList<string> roots)
    {
        return roots
            .SelectMany(root => SafeEnumerateDirectories(root, maxItems: 600))
            .Where(directory => IsEmptyDirectory(directory))
            .Take(50)
            .Select(directory => new CleanupAdvisorCandidate
            {
                Title = DisplayName(directory),
                Detail = $"Lege map: {directory}",
                Metric = "0 B",
                Category = "Lege map",
                Path = directory,
                Action = "Kan naar Prullenbak na controle",
                SafetyScore = 88
            });
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanOldProjectFolders(IReadOnlyList<string> roots)
    {
        return roots
            .SelectMany(root => SafeEnumerateDirectories(root, maxItems: 800))
            .Where(directory =>
                Directory.Exists(Path.Combine(directory, ".git"))
                || SafeEnumerateFiles(directory, SearchOption.TopDirectoryOnly, maxItems: 40).Any(file =>
                    file.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(file).Equals("package.json", StringComparison.OrdinalIgnoreCase)
                    || Path.GetFileName(file).Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase)))
            .Where(directory => (DateTime.Now - SafeLastWrite(directory)).TotalDays >= 90)
            .Take(40)
            .Select(directory => new CleanupAdvisorCandidate
            {
                Title = DisplayName(directory),
                Detail = $"Projectmap is lang niet gewijzigd: {SafeLastWrite(directory):dd-MM-yyyy}",
                Metric = FormatBytes(SafeDirectorySize(directory, maxFiles: 4000)),
                Category = "Projectarchief",
                Path = directory,
                Action = "Archiveer naar zip of externe backup",
                SizeBytes = SafeDirectorySize(directory, maxFiles: 4000),
                SafetyScore = 62
            });
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanSuspiciousDownloadSources(IReadOnlyList<string> roots)
    {
        foreach (var file in EnumerateAuditFiles(roots).Take(2000))
        {
            var source = TryReadZoneHost(file);
            if (source is null || IsCommonTrustedDomain(source))
            {
                continue;
            }

            yield return BuildFileCandidate(file, "Bron-reputatie", $"Downloadbron: {source}", "Extra controleren", 50);
        }
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanStartupItems()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        foreach (var item in ScanStartupItemsWindows())
        {
            yield return item;
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<CleanupAdvisorCandidate> ScanStartupItemsWindows()
    {
        const string runPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            RegistryKey? key = null;
            try
            {
                key = hive.OpenSubKey(runPath);
            }
            catch
            {
                continue;
            }

            using (key)
            {
                if (key is null)
                {
                    continue;
                }

                string[] valueNames;
                try
                {
                    valueNames = key.GetValueNames().Take(80).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var name in valueNames)
                {
                    var value = key.GetValue(name)?.ToString() ?? string.Empty;
                    yield return new CleanupAdvisorCandidate
                    {
                        Title = name,
                        Detail = value,
                        Metric = "Startup",
                        Category = "Opstart-audit",
                        Action = "Controleer impact in Taakbeheer",
                        SafetyScore = 58
                    };
                }
            }
        }
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanAppCacheLocations()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var roots = new[]
        {
            Path.Combine(local, "Temp"),
            Path.Combine(local, "Google", "Chrome", "User Data", "Default", "Cache"),
            Path.Combine(local, "Microsoft", "Edge", "User Data", "Default", "Cache"),
            Path.Combine(roaming, "discord", "Cache"),
            Path.Combine(roaming, "Microsoft", "Teams", "Cache"),
            Path.Combine(local, "CrashDumps")
        };

        return roots
            .Where(Directory.Exists)
            .Select(root =>
            {
                var size = SafeDirectorySize(root, maxFiles: 6000);
                return new CleanupAdvisorCandidate
                {
                    Title = DisplayName(root),
                    Detail = root,
                    Metric = FormatBytes(size),
                    Category = "App-cache",
                    Path = root,
                    Action = "Sluit app eerst, daarna controleren",
                    SizeBytes = size,
                    SafetyScore = root.Contains("Temp", StringComparison.OrdinalIgnoreCase) ? 82 : 68
                };
            })
            .Where(candidate => candidate.SizeBytes > 0);
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanLargeHiddenFiles(IReadOnlyList<string> roots)
    {
        return EnumerateAuditFiles(roots)
            .Select(SafeFileInfo)
            .Where(info => info is not null)
            .Select(info => info!)
            .Where(info =>
                info.Length >= 50L * 1024L * 1024L
                && (info.Attributes.HasFlag(FileAttributes.Hidden)
                    || info.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
                    || info.Extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase)
                    || info.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase)
                    || info.Extension.Equals(".dmp", StringComparison.OrdinalIgnoreCase)))
            .Take(50)
            .Select(info => new CleanupAdvisorCandidate
            {
                Title = info.Name,
                Detail = $"Verborgen/log/tmp/bak bestand: {info.FullName}",
                Metric = FormatBytes(info.Length),
                Category = "Verborgen groot",
                Path = info.FullName,
                Action = "Controleer voor opruimen",
                SizeBytes = info.Length,
                SafetyScore = 57
            });
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanOldInstallerVersions(IReadOnlyList<string> roots)
    {
        return EnumerateAuditFiles(roots)
            .Where(file => InstallerExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Select(file => new { File = file, Info = SafeFileInfo(file), Product = NormalizeInstallerName(Path.GetFileNameWithoutExtension(file)) })
            .Where(item => item.Info is not null && item.Product.Length >= 4)
            .GroupBy(item => item.Product, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() >= 2)
            .SelectMany(group => group
                .OrderByDescending(item => item.Info!.LastWriteTime)
                .Skip(1)
                .Take(4))
            .Take(50)
            .Select(item => BuildFileCandidate(item.File, "Installer-versies", "Oudere installer naast nieuwere versie gevonden", "Bewaar nieuwste, controleer oude", 78));
    }

    private static IEnumerable<CleanupAdvisorCandidate> ScanGenericNames(IReadOnlyList<string> roots)
    {
        return EnumerateAuditFiles(roots)
            .Where(file => Regex.IsMatch(
                Path.GetFileNameWithoutExtension(file),
                @"^(scan|img|image|photo|foto|screenshot|document|download|whatsapp|untitled)[\s_\-]?\d*$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Take(50)
            .Select(file => BuildFileCandidate(file, "Naam-normalisatie", "Generieke bestandsnaam", "Slim hernoemen", 84));
    }

    private static CleanupAdvisorCandidate BuildDownloadsHealthCandidate(IReadOnlyList<string> roots)
    {
        var files = EnumerateAuditFiles(roots).Take(5000).ToList();
        var oldFiles = files.Count(file => SafeFileInfo(file)?.LastWriteTime <= DateTime.Now.AddDays(-30));
        var largeFiles = files.Count(file => SafeFileInfo(file)?.Length >= 100L * 1024L * 1024L);
        var conflicts = files.Count(file => Path.GetFileName(file).Contains("(1)", StringComparison.OrdinalIgnoreCase));
        var score = Math.Clamp(100 - oldFiles / 5 - largeFiles * 2 - conflicts * 2, 0, 100);
        return new CleanupAdvisorCandidate
        {
            Title = "Gezondheidsscore Downloads",
            Detail = $"{files.Count} bestanden bekeken, {oldFiles} oud, {largeFiles} groot, {conflicts} mogelijke conflict-kopieën.",
            Metric = $"{score}/100",
            Category = "Healthscore",
            Action = score >= 80 ? "Gezond" : score >= 55 ? "Opruimscan aanbevolen" : "Grote opruimronde aanbevolen",
            SafetyScore = 95
        };
    }

    private static IReadOnlyList<string> ResolvePowerAuditRoots(IReadOnlyList<string>? roots)
    {
        if (roots is { Count: > 0 })
        {
            return roots;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var values = new List<string>();
        AddIfPresent(values, Path.Combine(userProfile, "Downloads"));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        return values;
    }

    private static IEnumerable<string> EnumerateAuditFiles(IReadOnlyList<string> roots)
    {
        return roots
            .Where(Directory.Exists)
            .SelectMany(root => SafeEnumerateFiles(root, SearchOption.AllDirectories, maxItems: 5000));
    }

    private static bool FileLooksSensitive(string path, IReadOnlyList<string> privacyHints)
    {
        var extension = Path.GetExtension(path);
        if (extension is not (".txt" or ".csv" or ".md" or ".json" or ".xml"))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[Math.Min(stream.Length, 64 * 1024)];
            var read = stream.Read(buffer);
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
            return privacyHints.Any(hint => text.Contains(hint, StringComparison.OrdinalIgnoreCase))
                || Regex.IsMatch(text, @"\bNL\d{2}[A-Z]{4}\d{10}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch
        {
            return false;
        }
    }

    private static string? TryReadShortcutTarget(string shortcutPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return null;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            return shortcut.TargetPath as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsEmptyDirectory(string directory)
    {
        try
        {
            return Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any();
        }
        catch
        {
            return false;
        }
    }

    private static string? TryReadZoneHost(string path)
    {
        try
        {
            var lines = File.ReadAllLines(path + ":Zone.Identifier");
            var url = lines
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0].Equals("HostUrl", StringComparison.OrdinalIgnoreCase)
                    || parts[0].Equals("ReferrerUrl", StringComparison.OrdinalIgnoreCase))
                .Select(parts => parts[1])
                .FirstOrDefault(value => Uri.TryCreate(value, UriKind.Absolute, out _));
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCommonTrustedDomain(string host)
    {
        string[] trusted =
        [
            "microsoft.com",
            "github.com",
            "google.com",
            "adobe.com",
            "nvidia.com",
            "amd.com",
            "intel.com",
            "mozilla.org",
            "steamstatic.com",
            "steampowered.com"
        ];

        return trusted.Any(domain =>
            host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeInstallerName(string name)
    {
        var withoutVersion = Regex.Replace(
            name,
            @"(\d+[._-]){1,}\d+|x64|x86|setup|installer|install|latest|win64|win32",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return Normalize(withoutVersion);
    }

    private static CleanupAdvisorCandidate BuildFileCandidate(
        string file,
        string category,
        string detail,
        string action,
        int safetyScore)
    {
        var info = SafeFileInfo(file);
        return new CleanupAdvisorCandidate
        {
            Title = Path.GetFileName(file),
            Detail = detail,
            Metric = info is null ? "Onbekend" : FormatBytes(info.Length),
            Category = category,
            Path = file,
            Action = action,
            SizeBytes = info?.Length ?? 0,
            SafetyScore = safetyScore
        };
    }

    private static IReadOnlyList<string> ResolveDriverScanRoots(IReadOnlyList<string>? roots)
    {
        if (roots is { Count: > 0 })
        {
            return roots;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var values = new List<string>();
        AddIfPresent(values, Path.Combine(userProfile, "Downloads"));
        AddIfPresent(values, @"C:\AMD");
        AddIfPresent(values, Path.Combine(programData, "NVIDIA Corporation", "Downloader"));
        AddIfPresent(values, Path.Combine(programData, "Intel", "Package Cache"));
        return values;
    }

    private static IReadOnlyList<string> ResolveAppDataRoots(IReadOnlyList<string>? roots)
    {
        if (roots is { Count: > 0 })
        {
            return roots;
        }

        var values = new List<string>();
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        AddIfPresent(values, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData",
            "LocalLow"));
        return values;
    }

    private static IReadOnlyList<string> ResolveStorageRoots(IReadOnlyList<string>? roots)
    {
        if (roots is { Count: > 0 })
        {
            return roots;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var values = new List<string>();
        AddIfPresent(values, Path.Combine(userProfile, "Downloads"));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        AddIfPresent(values, Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        AddIfPresent(values, Path.GetTempPath());
        return values;
    }

    private static bool IsKnownDriverCacheRoot(string root)
    {
        return root.Contains(@"\AMD", StringComparison.OrdinalIgnoreCase)
            || root.Contains("NVIDIA Corporation", StringComparison.OrdinalIgnoreCase)
            || root.Contains("Intel", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> LoadInstalledSoftwareKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!OperatingSystem.IsWindows())
        {
            return keys;
        }

        LoadInstalledSoftwareKeys(keys);
        return keys;
    }

    [SupportedOSPlatform("windows")]
    private static void LoadInstalledSoftwareKeys(HashSet<string> keys)
    {
        const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        {
            LoadInstalledSoftwareKeys(keys, hive, uninstallPath);
            LoadInstalledSoftwareKeys(keys, hive, $@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void LoadInstalledSoftwareKeys(HashSet<string> keys, RegistryKey hive, string path)
    {
        try
        {
            using var uninstallKey = hive.OpenSubKey(path);
            if (uninstallKey is null)
            {
                return;
            }

            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var appKey = uninstallKey.OpenSubKey(subKeyName);
                var displayName = appKey?.GetValue("DisplayName") as string;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    keys.Add(Normalize(displayName));
                }
            }
        }
        catch
        {
            // Some uninstall keys are not readable; this advisor is best-effort.
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path, int maxItems)
    {
        try
        {
            return Directory.Exists(path)
                ? Directory.EnumerateDirectories(path).Take(maxItems).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path, SearchOption option, int maxItems)
    {
        try
        {
            return Directory.Exists(path)
                ? Directory.EnumerateFiles(path, "*", option).Take(maxItems).ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }

    private static long SafeDirectorySize(string folder, int maxFiles)
    {
        long total = 0;
        foreach (var file in SafeEnumerateFiles(folder, SearchOption.AllDirectories, maxFiles))
        {
            try
            {
                total += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore files that disappear or are locked while scanning.
            }
        }

        return total;
    }

    private static FileInfo? SafeFileInfo(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime SafeLastWrite(string path)
    {
        try
        {
            return Directory.GetLastWriteTime(path);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static void AddIfPresent(List<string> values, string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            values.Add(path);
        }
    }

    private static string DisplayName(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? trimmed : name;
    }

    private static string Normalize(string value)
    {
        return string.Concat(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}

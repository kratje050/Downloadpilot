using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Persistence;

namespace DownloadPilot.Infrastructure.Services;

public sealed class GitHubUpdateService : IUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/kratje050/Downloadpilot/releases/latest";
    private static readonly string[] PreferredAssetExtensions = [".exe", ".msi", ".zip"];

    public async Task<UpdateCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DownloadPilot", currentVersion));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await client.GetAsync(LatestReleaseUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Message = "Geen GitHub-release gepubliceerd"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = currentVersion,
                    Message = $"Updatecheck mislukt: {(int)response.StatusCode}"
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var latestVersion = ReadString(root, "tag_name") ?? ReadString(root, "name");
            var releaseUrl = ReadString(root, "html_url");
            var asset = SelectUpdateAsset(root);
            var isNewer = IsNewerVersion(latestVersion, currentVersion);

            return new UpdateCheckResult
            {
                IsUpdateAvailable = isNewer,
                CurrentVersion = currentVersion,
                LatestVersion = latestVersion,
                ReleaseUrl = releaseUrl,
                DownloadUrl = asset.DownloadUrl,
                AssetName = asset.Name,
                Message = isNewer
                    ? $"Nieuwe versie beschikbaar: {latestVersion}"
                    : "DownloadPilot is up-to-date"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                CurrentVersion = currentVersion,
                Message = $"Updatecheck overgeslagen: {ex.Message}"
            };
        }
    }

    public async Task<string?> DownloadUpdateAsync(UpdateCheckResult update, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
        {
            return null;
        }

        var updateDirectory = Path.Combine(SqlitePaths.DataDirectory, "Updates");
        Directory.CreateDirectory(updateDirectory);
        var fileName = SanitizeFileName(update.AssetName ?? Path.GetFileName(new Uri(update.DownloadUrl).LocalPath));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"DownloadPilot-{update.LatestVersion ?? "update"}.zip";
        }

        var outputPath = Path.Combine(updateDirectory, fileName);
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DownloadPilot", update.CurrentVersion));
        await using var remote = await client.GetStreamAsync(update.DownloadUrl, cancellationToken);
        await using var local = File.Create(outputPath);
        await remote.CopyToAsync(local, cancellationToken);
        return outputPath;
    }

    public static bool IsNewerVersion(string? latestVersion, string currentVersion)
    {
        var latest = ParseVersion(latestVersion);
        var current = ParseVersion(currentVersion);
        return latest is not null && current is not null && latest > current;
    }

    public static bool StartDownloadedUpdate(
        string path,
        string? applicationDirectory = null,
        string? executablePath = null,
        int? currentProcessId = null)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(applicationDirectory)
            && !string.IsNullOrWhiteSpace(executablePath)
            && currentProcessId is > 0)
        {
            var scriptPath = Path.Combine(Path.GetDirectoryName(path) ?? Path.GetTempPath(), "Install-DownloadPilot-Update.cmd");
            File.WriteAllText(
                scriptPath,
                BuildPortableUpdateScript(path, applicationDirectory, executablePath, currentProcessId.Value));

            Process.Start(new ProcessStartInfo(scriptPath)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            return true;
        }

        var startInfo = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            ? new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"")
            : new ProcessStartInfo(path);
        startInfo.UseShellExecute = true;
        Process.Start(startInfo);
        return false;
    }

    private static string BuildPortableUpdateScript(
        string zipPath,
        string applicationDirectory,
        string executablePath,
        int processId)
    {
        var escapedZip = EscapeCmdValue(zipPath);
        var escapedDirectory = EscapeCmdValue(applicationDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var escapedExecutable = EscapeCmdValue(executablePath);
        return string.Join(
            Environment.NewLine,
            "@echo off",
            "setlocal",
            "timeout /t 1 /nobreak >nul",
            $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"try {{ Wait-Process -Id {processId} -Timeout 60 -ErrorAction SilentlyContinue }} catch {{ }}; Expand-Archive -LiteralPath '{escapedZip}' -DestinationPath '{escapedDirectory}' -Force; Start-Process -FilePath '{escapedExecutable}'\"",
            "endlocal",
            string.Empty);
    }

    private static string EscapeCmdValue(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static (string? Name, string? DownloadUrl) SelectUpdateAsset(JsonElement releaseRoot)
    {
        if (!releaseRoot.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return default;
        }

        foreach (var preferredExtension in PreferredAssetExtensions)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = ReadString(asset, "name");
                var downloadUrl = ReadString(asset, "browser_download_url");
                if (!string.IsNullOrWhiteSpace(name)
                    && !string.IsNullOrWhiteSpace(downloadUrl)
                    && name.EndsWith(preferredExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return (name, downloadUrl);
                }
            }
        }

        return default;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static Version? ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Trim().TrimStart('v', 'V');
        var suffixIndex = cleaned.IndexOfAny(['-', '+']);
        if (suffixIndex >= 0)
        {
            cleaned = cleaned[..suffixIndex];
        }

        return Version.TryParse(cleaned, out var version) ? version : null;
    }

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "0.1.0";
        var suffixIndex = version.IndexOf('+');
        return suffixIndex >= 0 ? version[..suffixIndex] : version;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalid.Contains(character) ? '_' : character));
    }
}

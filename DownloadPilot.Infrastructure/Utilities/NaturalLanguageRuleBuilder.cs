using System.Text.RegularExpressions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Core.Models;

namespace DownloadPilot.Infrastructure.Utilities;

public static partial class NaturalLanguageRuleBuilder
{
    public static (RuleDefinition Rule, string Feedback) Build(string instruction, string destinationRoot)
    {
        if (string.IsNullOrWhiteSpace(instruction))
        {
            throw new InvalidOperationException("Typ eerst wat de regel moet doen.");
        }

        var text = instruction.Trim();
        var category = DetectCategory(text);
        var extension = DetectExtension(text, category);
        var fileNameContains = DetectContains(text);
        var destination = DetectDestination(text, destinationRoot, category);
        var autoApply = ContainsAny(text, "automatisch", "altijd", "vanzelf", "zonder vragen");
        var priority = autoApply ? 90 : 82;
        var name = BuildName(category, extension, fileNameContains, destination);

        var rule = new RuleDefinition
        {
            Name = name,
            ExtensionEquals = extension,
            FileNameContains = fileNameContains,
            AutoApply = autoApply,
            Priority = priority,
            Category = category,
            DestinationFolder = destination,
            RenameTemplate = category is FileCategory.Facturen
                ? "{datum}_Factuur_{origineel}"
                : null
        };

        var feedback = $"Regel gemaakt: {name}. " +
            $"Categorie {category}, {(extension is null ? "alle extensies" : extension)}, " +
            $"doelmap {destination}.";
        return (rule, feedback);
    }

    private static FileCategory DetectCategory(string text)
    {
        if (ContainsAny(text, "factuur", "facturen", "bon", "bonnetje", "bonnetjes", "btw", "rekening"))
        {
            return FileCategory.Facturen;
        }

        if (ContainsAny(text, "foto", "fotos", "afbeelding", "afbeeldingen", "screenshot", "plaatje"))
        {
            return FileCategory.Afbeeldingen;
        }

        if (ContainsAny(text, "installer", "installatie", "setup", "programma", ".exe", ".msi"))
        {
            return FileCategory.Installatiebestanden;
        }

        if (ContainsAny(text, "zip", "rar", "archief", "archieven", ".7z"))
        {
            return FileCategory.Archieven;
        }

        if (ContainsAny(text, "video", "videos", "film", ".mp4", ".mov"))
        {
            return FileCategory.Videos;
        }

        if (ContainsAny(text, "muziek", "audio", ".mp3", ".wav"))
        {
            return FileCategory.Muziek;
        }

        if (ContainsAny(text, "school", "huiswerk", "studie"))
        {
            return FileCategory.School;
        }

        if (ContainsAny(text, "werk", "bedrijf", "project"))
        {
            return FileCategory.Werk;
        }

        return FileCategory.Documenten;
    }

    private static string? DetectExtension(string text, FileCategory category)
    {
        var explicitExtension = ExtensionRegex().Match(text);
        if (explicitExtension.Success)
        {
            return "." + explicitExtension.Groups["extension"].Value.ToLowerInvariant();
        }

        if (ContainsAny(text, "pdf"))
        {
            return ".pdf";
        }

        if (ContainsAny(text, "excel", "spreadsheet"))
        {
            return ".xlsx";
        }

        if (ContainsAny(text, "word"))
        {
            return ".docx";
        }

        return category switch
        {
            FileCategory.Installatiebestanden when ContainsAny(text, "msi") => ".msi",
            FileCategory.Installatiebestanden => ".exe",
            FileCategory.Archieven when ContainsAny(text, "rar") => ".rar",
            FileCategory.Archieven when ContainsAny(text, "7z") => ".7z",
            FileCategory.Archieven => ".zip",
            FileCategory.Videos => ".mp4",
            FileCategory.Muziek => ".mp3",
            _ => null
        };
    }

    private static string? DetectContains(string text)
    {
        foreach (var pattern in new[] { @"\bvan\s+(?<value>.+?)(?:\s+naar|\s+in|$)", @"\bbevat\s+(?<value>.+?)(?:\s+naar|\s+in|$)" })
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var value = CleanPhrase(match.Groups["value"].Value);
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    private static string DetectDestination(string text, string destinationRoot, FileCategory category)
    {
        var destination = DestinationRegex().Matches(text)
            .Cast<Match>()
            .LastOrDefault()?.Groups["value"].Value;

        if (string.IsNullOrWhiteSpace(destination))
        {
            return Path.Combine(destinationRoot, category.ToString());
        }

        var folderName = FileNameSanitizer.Sanitize(CleanPhrase(destination));
        return string.IsNullOrWhiteSpace(folderName)
            ? Path.Combine(destinationRoot, category.ToString())
            : Path.Combine(destinationRoot, folderName);
    }

    private static string BuildName(
        FileCategory category,
        string? extension,
        string? fileNameContains,
        string destination)
    {
        var parts = new List<string> { "AI", category.ToString() };
        if (!string.IsNullOrWhiteSpace(extension))
        {
            parts.Add(extension.TrimStart('.').ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(fileNameContains))
        {
            parts.Add(fileNameContains);
        }

        parts.Add("naar " + Path.GetFileName(destination.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
        return string.Join(" ", parts);
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static string CleanPhrase(string value)
    {
        return value
            .Replace("de map", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("map", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("automatisch", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("altijd", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("vanzelf", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("zonder vragen", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim(' ', '.', ',', ':', ';', '"', '\'');
    }

    [GeneratedRegex(@"\.(?<extension>[a-z0-9]{2,5})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionRegex();

    [GeneratedRegex(@"\b(?:naar|in)\s+(?<value>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DestinationRegex();
}

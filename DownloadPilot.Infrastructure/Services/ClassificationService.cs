using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;

namespace DownloadPilot.Infrastructure.Services;

public sealed class ClassificationService : IClassificationService
{
    public (FileCategory Category, int Confidence, string Reason) Classify(string filePath, string sourceFolder, string? extractedText = null)
    {
        var extension = Path.GetExtension(filePath);
        var fileName = Path.GetFileName(filePath);
        var textToInspect = $"{fileName} {extractedText}";

        if (ContainsAny(textToInspect, "factuur", "invoice", "btw", "bedrag"))
        {
            return (FileCategory.Facturen, 90, "Factuurtermen gevonden in bestandsnaam of inhoud");
        }

        if (Matches(extension, ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".heic", ".heif"))
        {
            return (FileCategory.Afbeeldingen, 95, "Afbeeldingsextensie herkend");
        }

        if (Matches(extension, ".mp4", ".mov", ".mkv", ".avi", ".webm"))
        {
            return (FileCategory.Videos, 95, "Video-extensie herkend");
        }

        if (Matches(extension, ".mp3", ".wav", ".flac", ".aac"))
        {
            return (FileCategory.Muziek, 92, "Muziekextensie herkend");
        }

        if (Matches(extension, ".exe", ".msi"))
        {
            return (FileCategory.Installatiebestanden, 98, "Installatiebestand herkend");
        }

        if (Matches(extension, ".zip", ".rar", ".7z", ".tar", ".gz"))
        {
            return (FileCategory.Archieven, 95, "Archiefextensie herkend");
        }

        if (Matches(extension, ".pdf", ".doc", ".docx", ".txt"))
        {
            if (ContainsAny(textToInspect, "handleiding", "manual", "gebruiksaanwijzing"))
            {
                return (FileCategory.Handleidingen, 86, "Handleidingstermen gevonden");
            }

            return (FileCategory.Documenten, 80, "Documentextensie herkend");
        }

        if (sourceFolder.Contains("school", StringComparison.OrdinalIgnoreCase))
        {
            return (FileCategory.School, 70, "Bronmap bevat school");
        }

        if (sourceFolder.Contains("werk", StringComparison.OrdinalIgnoreCase))
        {
            return (FileCategory.Werk, 70, "Bronmap bevat werk");
        }

        return (FileCategory.Overig, 50, "Geen specifieke regel gevonden");
    }

    private static bool Matches(string extension, params string[] candidates)
        => candidates.Any(c => extension.Equals(c, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsAny(string text, params string[] candidates)
        => candidates.Any(c => text.Contains(c, StringComparison.OrdinalIgnoreCase));
}

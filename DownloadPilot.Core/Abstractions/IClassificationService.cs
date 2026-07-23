using DownloadPilot.Core.Enums;

namespace DownloadPilot.Core.Abstractions;

public interface IClassificationService
{
    (FileCategory Category, int Confidence, string Reason) Classify(string filePath, string sourceFolder, string? extractedText = null);
}

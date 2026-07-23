namespace DownloadPilot.Infrastructure.Utilities;

public static class FileNameSanitizer
{
    public static string Sanitize(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        sanitized = sanitized.Replace("  ", " ").Trim(' ', '.');
        return string.IsNullOrWhiteSpace(sanitized) ? "onbekend" : sanitized;
    }

    public static string BuildUniquePath(string targetFolder, string desiredFileName)
    {
        var extension = Path.GetExtension(desiredFileName);
        var fileBase = Path.GetFileNameWithoutExtension(desiredFileName);
        var candidate = Path.Combine(targetFolder, desiredFileName);

        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(targetFolder, $"{fileBase} ({index}){extension}");
            index++;
        }

        return candidate;
    }
}

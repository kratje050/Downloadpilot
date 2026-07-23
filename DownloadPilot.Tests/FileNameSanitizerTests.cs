using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class FileNameSanitizerTests
{
    [Fact]
    public void Sanitize_ShouldReplaceInvalidCharacters()
    {
        var sanitized = FileNameSanitizer.Sanitize("factuur:coolblue?.pdf");

        Assert.DoesNotContain(':', sanitized);
        Assert.DoesNotContain('?', sanitized);
    }

    [Fact]
    public void BuildUniquePath_ShouldAvoidDuplicateNames()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var existing = Path.Combine(tempDir, "bestand.pdf");
            File.WriteAllText(existing, "x");

            var unique = FileNameSanitizer.BuildUniquePath(tempDir, "bestand.pdf");

            Assert.NotEqual(existing, unique);
            Assert.EndsWith(".pdf", unique, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}

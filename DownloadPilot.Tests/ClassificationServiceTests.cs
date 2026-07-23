using DownloadPilot.Core.Enums;
using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class ClassificationServiceTests
{
    private readonly ClassificationService _service = new();

    [Fact]
    public void Classify_ShouldMarkImageExtensionsAsAfbeeldingen()
    {
        var result = _service.Classify("foto.jpg", "C:\\Users\\test\\Downloads");

        Assert.Equal(FileCategory.Afbeeldingen, result.Category);
        Assert.True(result.Confidence >= 90);
    }

    [Fact]
    public void Classify_ShouldRecognizeInvoiceTermsInPdfText()
    {
        var result = _service.Classify("document.pdf", "C:\\Users\\test\\Downloads", "Dit is een factuur met btw en bedrag");

        Assert.Equal(FileCategory.Facturen, result.Category);
        Assert.Contains("Factuur", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Classify_ShouldRecognizeInvoiceTermsFromOcrOnImage()
    {
        var result = _service.Classify("scan.jpg", "C:\\Users\\test\\Downloads", "Factuur leverancier Coolblue bedrag 42,00");

        Assert.Equal(FileCategory.Facturen, result.Category);
    }
}

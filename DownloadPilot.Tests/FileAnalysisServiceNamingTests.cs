using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Infrastructure.Services;

namespace DownloadPilot.Tests;

public sealed class FileAnalysisServiceNamingTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldBuildReadableInvoiceNameFromExtractedText()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var filePath = Path.Combine(tempRoot, "document(14).pdf");
        await File.WriteAllTextAsync(filePath, "fake pdf file");

        try
        {
            IClassificationService classification = new ClassificationService();
            IPdfTextExtractionService pdf = new FakePdfTextExtractionService("Factuur\nLeverancier: Coolblue\nBedrag: 699,00\nFactuurdatum: 2026-07-22\nProduct: Wasmachine");
            IOcrService ocr = new FakeOcrService();
            var service = new FileAnalysisService(classification, pdf, ocr);

            var result = await service.AnalyzeAsync(filePath, tempRoot, CancellationToken.None);

            Assert.Equal(FileCategory.Facturen, result.SuggestedCategory);
            Assert.Contains("2026-07-22", result.SuggestedFileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Factuur", result.SuggestedFileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Coolblue", result.SuggestedFileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("699-00", result.SuggestedFileName, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".pdf", result.SuggestedFileName, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldUseCompanyNameAsInvoiceSubfolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var filePath = Path.Combine(tempRoot, "factuur.pdf");
        await File.WriteAllTextAsync(filePath, "fake pdf file");

        try
        {
            IClassificationService classification = new ClassificationService();
            IPdfTextExtractionService pdf = new FakePdfTextExtractionService("Factuur\nLeverancier: Coolblue\nBedrag: 42,00");
            IOcrService ocr = new FakeOcrService();
            var service = new FileAnalysisService(classification, pdf, ocr);

            var result = await service.AnalyzeAsync(filePath, tempRoot, CancellationToken.None);

            Assert.Equal(FileCategory.Facturen, result.SuggestedCategory);
            Assert.EndsWith(Path.Combine("Facturen", "Coolblue"), result.SuggestedDestinationFolder, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Slimme map", result.Reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldUseImageContentAsSubfolder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var filePath = Path.Combine(tempRoot, "screen.jpg");
        await File.WriteAllTextAsync(filePath, "fake image file");

        try
        {
            IClassificationService classification = new ClassificationService();
            IPdfTextExtractionService pdf = new FakePdfTextExtractionService(null);
            IOcrService ocr = new FakeOcrService("Schermopname van planning en notities");
            var service = new FileAnalysisService(classification, pdf, ocr);

            var result = await service.AnalyzeAsync(filePath, tempRoot, CancellationToken.None);

            Assert.Equal(FileCategory.Afbeeldingen, result.SuggestedCategory);
            Assert.EndsWith(Path.Combine("Afbeeldingen", "Screenshots"), result.SuggestedDestinationFolder, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Onderwerp herkend", result.Reason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private sealed class FakePdfTextExtractionService(string? text) : IPdfTextExtractionService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(text);
    }

    private sealed class FakeOcrService(string? text = null) : IOcrService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(text);
    }
}

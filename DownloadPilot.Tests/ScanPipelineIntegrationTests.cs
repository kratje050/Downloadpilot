using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Enums;
using DownloadPilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DownloadPilot.Tests;

public sealed class ScanPipelineIntegrationTests
{
    [Fact]
    public async Task ScanPipeline_ShouldProcessStableFilesAndClassifyCorrectly()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var invoicePdf = Path.Combine(root, "factuur_coolblue.pdf");
        var imageFile = Path.Combine(root, "vakantie.jpg");
        var installer = Path.Combine(root, "setup.exe");

        await File.WriteAllTextAsync(invoicePdf, "dummy");
        await File.WriteAllTextAsync(imageFile, "dummy");
        await File.WriteAllTextAsync(installer, "dummy");

        try
        {
            IFileStabilityService stability = new FileStabilityService(NullLogger<FileStabilityService>.Instance);
            IClassificationService classification = new ClassificationService();
            IPdfTextExtractionService pdf = new FakePdfTextExtractionService();
            IOcrService ocr = new FakeOcrService();
            IFileAnalysisService analysis = new FileAnalysisService(classification, pdf, ocr);

            var results = new List<(string Name, FileCategory Category)>();
            foreach (var file in Directory.EnumerateFiles(root))
            {
                var stable = await stability.WaitUntilStableAsync(file, CancellationToken.None);
                Assert.True(stable);

                var analyzed = await analysis.AnalyzeAsync(file, root, CancellationToken.None);
                results.Add((analyzed.OriginalFileName, analyzed.SuggestedCategory));
            }

            Assert.Contains(results, r => r.Name == "factuur_coolblue.pdf" && r.Category == FileCategory.Facturen);
            Assert.Contains(results, r => r.Name == "vakantie.jpg" && r.Category == FileCategory.Afbeeldingen);
            Assert.Contains(results, r => r.Name == "setup.exe" && r.Category == FileCategory.Installatiebestanden);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakePdfTextExtractionService : IPdfTextExtractionService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeOcrService : IOcrService
    {
        public Task<string?> TryExtractTextAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }
}

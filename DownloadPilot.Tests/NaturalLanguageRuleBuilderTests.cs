using DownloadPilot.Core.Enums;
using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class NaturalLanguageRuleBuilderTests
{
    [Fact]
    public void Build_ShouldCreateInvoiceRuleFromDutchInstruction()
    {
        var result = NaturalLanguageRuleBuilder.Build(
            "Zet alle bonnetjes van Albert Heijn automatisch in boodschappen",
            "C:\\DownloadPilot");

        Assert.Equal(FileCategory.Facturen, result.Rule.Category);
        Assert.Equal("Albert Heijn", result.Rule.FileNameContains);
        Assert.True(result.Rule.AutoApply);
        Assert.Equal("C:\\DownloadPilot\\boodschappen", result.Rule.DestinationFolder);
    }

    [Fact]
    public void Build_ShouldCreateArchiveRuleForZipFiles()
    {
        var result = NaturalLanguageRuleBuilder.Build(
            "zip bestanden naar archieven",
            "C:\\DownloadPilot");

        Assert.Equal(FileCategory.Archieven, result.Rule.Category);
        Assert.Equal(".zip", result.Rule.ExtensionEquals);
        Assert.Equal("C:\\DownloadPilot\\archieven", result.Rule.DestinationFolder);
    }
}

using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class DocumentInsightsExtractorTests
{
    [Fact]
    public void Extract_ShouldRecognizeInvoiceCompanyAmountAndDate()
    {
        const string text = "Factuur\nLeverancier: Coolblue\nBedrag: 699,00\nFactuurdatum: 2026-07-22\nProduct: Laptop";

        var insights = DocumentInsightsExtractor.Extract(text);

        Assert.Equal("Factuur", insights.DocumentType);
        Assert.Equal("Coolblue", insights.CompanyName);
        Assert.Equal(699.00m, insights.Amount);
        Assert.Equal(new DateTime(2026, 7, 22), insights.DocumentDate);
        Assert.Equal("Laptop", insights.Subject);
    }

    [Fact]
    public void DetectDocumentType_ShouldRecognizeInsuranceDocument()
    {
        var docType = DocumentInsightsExtractor.DetectDocumentType("Uw verzekering polis en premie details");

        Assert.Equal("Verzekeringsdocument", docType);
    }

    [Fact]
    public void Extract_ShouldRecognizeCompanyNameFromBusinessHeader()
    {
        const string text = "Acme B.V.\nFactuur\nTotaalbedrag 129,95\nwww.acme.nl";

        var insights = DocumentInsightsExtractor.Extract(text);

        Assert.Equal("Acme B.V.", insights.CompanyName);
    }
}

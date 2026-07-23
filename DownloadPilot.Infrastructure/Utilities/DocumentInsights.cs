namespace DownloadPilot.Infrastructure.Utilities;

public sealed class DocumentInsights
{
    public string? DocumentType { get; init; }

    public string? CompanyName { get; init; }

    public decimal? Amount { get; init; }

    public DateTime? DocumentDate { get; init; }

    public string? Subject { get; init; }
}

namespace DownloadPilot.App.ViewModels;

public sealed class InvoiceDashboardItemViewModel
{
    public required string Company { get; init; }

    public required string Period { get; init; }

    public int Count { get; init; }

    public decimal TotalAmount { get; init; }

    public string TotalAmountReadable => TotalAmount <= 0 ? "-" : $"EUR {TotalAmount:0.00}";

    public required string Examples { get; init; }
}

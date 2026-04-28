namespace FundingPlatform.Application.Options;

public sealed class AdminReportsOptions
{
    public const string SectionName = "AdminReports";

    public string? DefaultCurrency { get; set; }
    public int CsvRowLimit { get; set; } = 50_000;
}

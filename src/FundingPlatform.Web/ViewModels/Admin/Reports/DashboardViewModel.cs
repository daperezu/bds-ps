using FundingPlatform.Application.Admin.Reports.DTOs;

namespace FundingPlatform.Web.ViewModels.Admin.Reports;

public sealed class DashboardViewModel
{
    public required DateRange AppliedRange { get; init; }
    public required IReadOnlyList<KpiTileViewModel> PipelineTiles { get; init; }
    public required IReadOnlyList<KpiTileViewModel> FinancialTiles { get; init; }
    public required IReadOnlyList<KpiTileViewModel> ApplicantTiles { get; init; }
}

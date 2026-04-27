using FundingPlatform.Application.Admin.Reports.DTOs;

namespace FundingPlatform.Web.ViewModels.Admin.Reports;

public sealed class AgingApplicationsViewModel
{
    public required ListAgingApplicationsResult Result { get; init; }
    public required int PageSize { get; init; }
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
}

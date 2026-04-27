using FundingPlatform.Application.Admin.Reports.DTOs;

namespace FundingPlatform.Application.Interfaces;

public interface IReportQueryService
{
    IQueryable<ApplicationRowDto>      ApplicationsQuery     (ListApplicationsRequest      req);
    IQueryable<ApplicantRowDto>        ApplicantsQuery       (ListApplicantsRequest        req);
    IQueryable<FundedItemRowDto>       FundedItemsQuery      (ListFundedItemsRequest       req);
    IQueryable<AgingApplicationRowDto> AgingApplicationsQuery(ListAgingApplicationsRequest req);

    Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default);
}

using FundingPlatform.Application.Admin.Reports.DTOs;

namespace FundingPlatform.Application.Admin.Reports;

public interface IAdminReportsService
{
    Task<DashboardResult> GetDashboardAsync(DateRange? range, CancellationToken ct = default);

    Task<ListApplicationsResult>      ListApplicationsAsync     (ListApplicationsRequest      req, CancellationToken ct = default);
    Task<ListApplicantsResult>        ListApplicantsAsync       (ListApplicantsRequest        req, CancellationToken ct = default);
    Task<ListFundedItemsResult>       ListFundedItemsAsync      (ListFundedItemsRequest       req, CancellationToken ct = default);
    Task<ListAgingApplicationsResult> ListAgingApplicationsAsync(ListAgingApplicationsRequest req, CancellationToken ct = default);

    IAsyncEnumerable<string> ExportApplicationsCsvAsync     (ListApplicationsRequest      req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportApplicantsCsvAsync       (ListApplicantsRequest        req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportFundedItemsCsvAsync      (ListFundedItemsRequest       req, CancellationToken ct = default);
    IAsyncEnumerable<string> ExportAgingApplicationsCsvAsync(ListAgingApplicationsRequest req, CancellationToken ct = default);
}

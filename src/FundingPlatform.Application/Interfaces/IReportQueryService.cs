using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Domain.ValueObjects;

namespace FundingPlatform.Application.Interfaces;

/// <summary>
/// Infrastructure-layer contract for executing the report queries. Each list
/// method materializes a sorted page directly so the Application layer does not
/// take a dependency on EntityFrameworkCore extension methods.
/// </summary>
public interface IReportQueryService
{
    Task<int> CountApplicationsAsync(ListApplicationsRequest req, CancellationToken ct = default);

    Task<IReadOnlyList<ApplicationProjection>> ListApplicationsPageAsync(
        ListApplicationsRequest req, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Iterate the entire (unpaged) Applications result for CSV streaming.</summary>
    IAsyncEnumerable<ApplicationProjection> StreamApplicationsAsync(
        ListApplicationsRequest req, CancellationToken ct = default);

    /// <summary>
    /// Per-(application, currency) Approved-amount totals for the given application IDs.
    /// </summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<CurrencyAmount>>>
        ApplicationsApprovedTotalsAsync(IReadOnlyCollection<int> appIds, CancellationToken ct = default);

    Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default);
}

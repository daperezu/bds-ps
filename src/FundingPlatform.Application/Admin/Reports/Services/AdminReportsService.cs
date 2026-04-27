using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FundingPlatform.Application.Admin.Reports.Services;

public sealed class AdminReportsService : IAdminReportsService
{
    public const int PageSize = 25;

    private readonly IReportQueryService _queryService;
    private readonly IOptions<AdminReportsOptions> _options;
    private readonly ILogger<AdminReportsService> _logger;

    public AdminReportsService(
        IReportQueryService queryService,
        IOptions<AdminReportsOptions> options,
        ILogger<AdminReportsService> logger)
    {
        _queryService = queryService;
        _options = options;
        _logger = logger;
    }

    public Task<DashboardResult> GetDashboardAsync(DateRange? range, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolved = range ?? new DateRange(today.AddDays(-30), today);
        return _queryService.DashboardSnapshotAsync(resolved, ct);
    }

    public Task<ListApplicationsResult> ListApplicationsAsync(ListApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US3.");

    public Task<ListApplicantsResult> ListApplicantsAsync(ListApplicantsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US4.");

    public Task<ListFundedItemsResult> ListFundedItemsAsync(ListFundedItemsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US5.");

    public Task<ListAgingApplicationsResult> ListAgingApplicationsAsync(ListAgingApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US6.");

    public IAsyncEnumerable<string> ExportApplicationsCsvAsync(ListApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US3.");

    public IAsyncEnumerable<string> ExportApplicantsCsvAsync(ListApplicantsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US4.");

    public IAsyncEnumerable<string> ExportFundedItemsCsvAsync(ListFundedItemsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US5.");

    public IAsyncEnumerable<string> ExportAgingApplicationsCsvAsync(ListAgingApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US6.");
}

using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Application.Interfaces;

namespace FundingPlatform.Infrastructure.Persistence.Reports;

public sealed class ReportQueryService : IReportQueryService
{
    private readonly AppDbContext _db;

    public ReportQueryService(AppDbContext db)
    {
        _db = db;
    }

    public IQueryable<ApplicationRowDto> ApplicationsQuery(ListApplicationsRequest req)
        => throw new NotImplementedException("Implemented in user-story phase US3.");

    public IQueryable<ApplicantRowDto> ApplicantsQuery(ListApplicantsRequest req)
        => throw new NotImplementedException("Implemented in user-story phase US4.");

    public IQueryable<FundedItemRowDto> FundedItemsQuery(ListFundedItemsRequest req)
        => throw new NotImplementedException("Implemented in user-story phase US5.");

    public IQueryable<AgingApplicationRowDto> AgingApplicationsQuery(ListAgingApplicationsRequest req)
        => throw new NotImplementedException("Implemented in user-story phase US6.");

    public Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US2.");
}

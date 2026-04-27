using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

    public async Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default)
    {
        var fromUtc = range.From.ToDateTime(TimeOnly.MinValue);
        var toUtc = range.To.ToDateTime(TimeOnly.MaxValue);

        // Pipeline: count applications per state (always current, excluding Draft).
        var pipelineRaw = await _db.Applications
            .AsNoTracking()
            .Where(a => a.State != ApplicationState.Draft)
            .GroupBy(a => a.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var pipeline = Enum.GetValues<ApplicationState>()
            .Where(s => s != ApplicationState.Draft)
            .Select(s => new PipelineCount(s, pipelineRaw.FirstOrDefault(p => p.State == s)?.Count ?? 0))
            .ToList();

        // Financial: approved this period (per currency).
        var approvedRaw = await _db.Items
            .AsNoTracking()
            .Where(i => i.ReviewStatus == ItemReviewStatus.Approved
                     && i.SelectedSupplierId != null)
            .Join(_db.Applications.AsNoTracking(),
                i => i.ApplicationId,
                a => a.Id,
                (i, a) => new { Item = i, App = a })
            .Where(x => x.App.State != ApplicationState.Draft
                     && x.App.State != ApplicationState.Submitted
                     && x.App.State != ApplicationState.UnderReview
                     && x.App.UpdatedAt >= fromUtc && x.App.UpdatedAt <= toUtc)
            .Join(_db.Quotations.AsNoTracking(),
                x => new { ItemId = x.Item.Id, SupplierId = x.Item.SelectedSupplierId!.Value },
                q => new { ItemId = q.ItemId, SupplierId = q.SupplierId },
                (x, q) => new { x.App.State, q.Currency, q.Price })
            .ToListAsync(ct);

        var approvedStack = approvedRaw
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyAmount(g.Key, g.Sum(r => r.Price)))
            .OrderBy(c => c.Currency)
            .ToList();

        var executedStack = approvedRaw
            .Where(r => r.State == ApplicationState.AgreementExecuted)
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyAmount(g.Key, g.Sum(r => r.Price)))
            .OrderBy(c => c.Currency)
            .ToList();

        var pendingStack = approvedStack
            .Select(a =>
            {
                var executed = executedStack.FirstOrDefault(e => e.Currency == a.Currency)?.Amount ?? 0m;
                return new CurrencyAmount(a.Currency, a.Amount - executed);
            })
            .Where(c => c.Amount > 0)
            .ToList();

        var financial = new List<FinancialKpi>
        {
            new("Approved this period",  approvedStack),
            new("Executed this period",  executedStack),
            new("Pending execution",     pendingStack),
        };

        // Applicant counts.
        var nonTerminalStates = new[]
        {
            ApplicationState.Draft,
            ApplicationState.Submitted,
            ApplicationState.UnderReview,
            ApplicationState.Resolved,
            ApplicationState.ResponseFinalized,
            ApplicationState.AppealOpen,
        };

        var activeApplicants = await _db.Applicants
            .AsNoTracking()
            .CountAsync(a => a.Applications.Any(app => nonTerminalStates.Contains(app.State)), ct);

        var repeatApplicants = await _db.Applicants
            .AsNoTracking()
            .CountAsync(a => a.Applications.Count(app => app.SubmittedAt != null) >= 2, ct);

        var newThisPeriod = await _db.Applicants
            .AsNoTracking()
            .Where(a => a.Applications.Any(app => app.SubmittedAt != null))
            .Select(a => new
            {
                MinSubmitted = a.Applications
                    .Where(app => app.SubmittedAt != null)
                    .Min(app => app.SubmittedAt)
            })
            .CountAsync(x => x.MinSubmitted >= fromUtc && x.MinSubmitted <= toUtc, ct);

        var applicants = new List<ApplicantKpi>
        {
            new("Active applicants",   activeApplicants),
            new("Repeat applicants",   repeatApplicants),
            new("New this period",     newThisPeriod),
        };

        return new DashboardResult(range, pipeline, financial, applicants);
    }
}

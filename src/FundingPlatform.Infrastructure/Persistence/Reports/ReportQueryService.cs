using System.Runtime.CompilerServices;
using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Domain.Entities;
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

    public Task<int> CountApplicationsAsync(ListApplicationsRequest req, CancellationToken ct = default)
        => BuildApplicationsQuery(req).CountAsync(ct);

    public async Task<IReadOnlyList<ApplicationProjection>> ListApplicationsPageAsync(
        ListApplicationsRequest req, int page, int pageSize, CancellationToken ct = default)
    {
        var q = ApplyApplicationsSort(BuildApplicationsQuery(req), req.Sort);
        return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async IAsyncEnumerable<ApplicationProjection> StreamApplicationsAsync(
        ListApplicationsRequest req, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var q = ApplyApplicationsSort(BuildApplicationsQuery(req), req.Sort);
        await foreach (var row in q.AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return row;
        }
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<CurrencyAmount>>>
        ApplicationsApprovedTotalsAsync(IReadOnlyCollection<int> appIds, CancellationToken ct = default)
    {
        if (appIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<CurrencyAmount>>();
        }

        var rows = await (
            from i in _db.Items.AsNoTracking()
            where appIds.Contains(i.ApplicationId)
                  && i.ReviewStatus == ItemReviewStatus.Approved
                  && i.SelectedSupplierId != null
            join q in _db.Quotations.AsNoTracking()
                on new { ItemId = i.Id, SupplierId = i.SelectedSupplierId!.Value }
                equals new { ItemId = q.ItemId, SupplierId = q.SupplierId }
            select new { i.ApplicationId, q.Currency, q.Price }
        ).ToListAsync(ct);

        return rows
            .GroupBy(r => r.ApplicationId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CurrencyAmount>)g
                    .GroupBy(r => r.Currency)
                    .Select(cg => new CurrencyAmount(cg.Key, cg.Sum(x => x.Price)))
                    .OrderBy(c => c.Currency)
                    .ToList());
    }

    public Task<int> CountApplicantsAsync(ListApplicantsRequest req, CancellationToken ct = default)
        => BuildApplicantsQuery(req).CountAsync(ct);

    public async Task<IReadOnlyList<ApplicantProjection>> ListApplicantsPageAsync(
        ListApplicantsRequest req, int page, int pageSize, CancellationToken ct = default)
    {
        var q = ApplyApplicantsSort(BuildApplicantsQuery(req), req.Sort);
        return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<CurrencyAmount>>>
        ApplicantsApprovedTotalsAsync(IReadOnlyCollection<int> applicantIds, CancellationToken ct = default)
    {
        if (applicantIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<CurrencyAmount>>();
        }

        var rows = await (
            from i in _db.Items.AsNoTracking()
            where i.ReviewStatus == ItemReviewStatus.Approved && i.SelectedSupplierId != null
            join a in _db.Applications.AsNoTracking() on i.ApplicationId equals a.Id
            where applicantIds.Contains(a.ApplicantId)
            join q in _db.Quotations.AsNoTracking()
                on new { ItemId = i.Id, SupplierId = i.SelectedSupplierId!.Value }
                equals new { ItemId = q.ItemId, SupplierId = q.SupplierId }
            select new { a.ApplicantId, q.Currency, q.Price }
        ).ToListAsync(ct);

        return rows
            .GroupBy(r => r.ApplicantId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CurrencyAmount>)g
                    .GroupBy(r => r.Currency)
                    .Select(cg => new CurrencyAmount(cg.Key, cg.Sum(x => x.Price)))
                    .OrderBy(c => c.Currency)
                    .ToList());
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<CurrencyAmount>>>
        ApplicantsExecutedTotalsAsync(IReadOnlyCollection<int> applicantIds, CancellationToken ct = default)
    {
        if (applicantIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<CurrencyAmount>>();
        }

        var rows = await (
            from i in _db.Items.AsNoTracking()
            where i.ReviewStatus == ItemReviewStatus.Approved && i.SelectedSupplierId != null
            join a in _db.Applications.AsNoTracking() on i.ApplicationId equals a.Id
            where applicantIds.Contains(a.ApplicantId)
                  && a.State == ApplicationState.AgreementExecuted
            join q in _db.Quotations.AsNoTracking()
                on new { ItemId = i.Id, SupplierId = i.SelectedSupplierId!.Value }
                equals new { ItemId = q.ItemId, SupplierId = q.SupplierId }
            select new { a.ApplicantId, q.Currency, q.Price }
        ).ToListAsync(ct);

        return rows
            .GroupBy(r => r.ApplicantId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CurrencyAmount>)g
                    .GroupBy(r => r.Currency)
                    .Select(cg => new CurrencyAmount(cg.Key, cg.Sum(x => x.Price)))
                    .OrderBy(c => c.Currency)
                    .ToList());
    }

    public async Task<DashboardResult> DashboardSnapshotAsync(DateRange range, CancellationToken ct = default)
    {
        var fromUtc = range.From.ToDateTime(TimeOnly.MinValue);
        var toUtc = range.To.ToDateTime(TimeOnly.MaxValue);

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

    private IQueryable<ApplicationProjection> BuildApplicationsQuery(ListApplicationsRequest req)
    {
        var q = _db.Applications.AsNoTracking().AsQueryable();

        if (req.States is { Count: > 0 } states)
        {
            q = q.Where(a => states.Contains(a.State));
        }

        if (req.From.HasValue)
        {
            var fromUtc = req.From.Value.ToDateTime(TimeOnly.MinValue);
            q = q.Where(a => a.UpdatedAt >= fromUtc);
        }

        if (req.To.HasValue)
        {
            var toUtc = req.To.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(a => a.UpdatedAt <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var pattern = $"%{req.Search.Trim()}%";
            q = q.Where(a =>
                EF.Functions.Like(a.Applicant.FirstName + " " + a.Applicant.LastName, pattern)
                || EF.Functions.Like(a.Applicant.LegalId, pattern)
                || EF.Functions.Like(a.Applicant.Email, pattern));
        }

        if (req.HasAgreement.HasValue)
        {
            q = req.HasAgreement.Value
                ? q.Where(a => a.FundingAgreement != null)
                : q.Where(a => a.FundingAgreement == null);
        }

        if (req.HasActiveAppeal.HasValue)
        {
            q = req.HasActiveAppeal.Value
                ? q.Where(a => a.Appeals.Any(ap => ap.Status == AppealStatus.Open))
                : q.Where(a => !a.Appeals.Any(ap => ap.Status == AppealStatus.Open));
        }

        return q.Select(a => new ApplicationProjection(
            a.Id,
            (a.Applicant.FirstName + " " + a.Applicant.LastName).Trim(),
            a.Applicant.LegalId,
            a.State,
            a.CreatedAt,
            a.SubmittedAt,
            a.UpdatedAt,
            a.Items.Count,
            a.FundingAgreement != null,
            a.Appeals.Any(ap => ap.Status == AppealStatus.Open)
        ));
    }

    private IQueryable<ApplicantProjection> BuildApplicantsQuery(ListApplicantsRequest req)
    {
        var q = _db.Applicants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var pattern = $"%{req.Search.Trim()}%";
            q = q.Where(a =>
                EF.Functions.Like(a.FirstName + " " + a.LastName, pattern)
                || EF.Functions.Like(a.LegalId, pattern)
                || EF.Functions.Like(a.Email, pattern));
        }

        if (req.HasExecutedAgreement.HasValue)
        {
            q = req.HasExecutedAgreement.Value
                ? q.Where(a => a.Applications.Any(app => app.State == ApplicationState.AgreementExecuted))
                : q.Where(a => !a.Applications.Any(app => app.State == ApplicationState.AgreementExecuted));
        }

        if (req.LastActivityFrom.HasValue)
        {
            var fromUtc = req.LastActivityFrom.Value.ToDateTime(TimeOnly.MinValue);
            q = q.Where(a => a.Applications.Any() && a.Applications.Max(app => app.UpdatedAt) >= fromUtc);
        }

        if (req.LastActivityTo.HasValue)
        {
            var toUtc = req.LastActivityTo.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(a => a.Applications.Any() && a.Applications.Max(app => app.UpdatedAt) <= toUtc);
        }

        return q.Select(a => new ApplicantProjection(
            a.Id,
            (a.FirstName + " " + a.LastName).Trim(),
            a.LegalId,
            a.Email,
            a.Applications.Count(app => app.SubmittedAt != null),
            a.Applications.Count(app => app.State == ApplicationState.Resolved),
            a.Applications.Count(app => app.State == ApplicationState.ResponseFinalized),
            a.Applications.Count(app => app.State == ApplicationState.AgreementExecuted),
            a.Applications.Sum(app => app.Items.Count),
            a.Applications.Sum(app => app.Items.Count(i => i.ReviewStatus == ItemReviewStatus.Approved)),
            a.Applications.Any() ? (DateTime?)a.Applications.Max(app => app.UpdatedAt) : null
        ));
    }

    private static IQueryable<ApplicantProjection> ApplyApplicantsSort(
        IQueryable<ApplicantProjection> q, string? sort)
    {
        return sort switch
        {
            "executed-asc"     => q.OrderBy(a => a.AgreementExecutedCount).ThenBy(a => a.ApplicantId),
            "approved-desc"    => q.OrderByDescending(a => a.ApprovedItems).ThenByDescending(a => a.ApplicantId),
            "approved-asc"     => q.OrderBy(a => a.ApprovedItems).ThenBy(a => a.ApplicantId),
            "applicant-asc"    => q.OrderBy(a => a.FullName).ThenBy(a => a.ApplicantId),
            "applicant-desc"   => q.OrderByDescending(a => a.FullName).ThenByDescending(a => a.ApplicantId),
            "lastActivity-desc"=> q.OrderByDescending(a => a.LastActivity).ThenByDescending(a => a.ApplicantId),
            "lastActivity-asc" => q.OrderBy(a => a.LastActivity).ThenBy(a => a.ApplicantId),
            _                  => q.OrderByDescending(a => a.AgreementExecutedCount).ThenByDescending(a => a.ApplicantId),
        };
    }

    private static IQueryable<ApplicationProjection> ApplyApplicationsSort(
        IQueryable<ApplicationProjection> q, string? sort)
    {
        return sort switch
        {
            "updated-asc"     => q.OrderBy(a => a.UpdatedAt).ThenBy(a => a.AppId),
            "submitted-desc"  => q.OrderByDescending(a => a.SubmittedAt).ThenByDescending(a => a.AppId),
            "submitted-asc"   => q.OrderBy(a => a.SubmittedAt).ThenBy(a => a.AppId),
            "applicant-asc"   => q.OrderBy(a => a.ApplicantFullName).ThenBy(a => a.AppId),
            "applicant-desc" => q.OrderByDescending(a => a.ApplicantFullName).ThenByDescending(a => a.AppId),
            _                 => q.OrderByDescending(a => a.UpdatedAt).ThenByDescending(a => a.AppId),
        };
    }
}

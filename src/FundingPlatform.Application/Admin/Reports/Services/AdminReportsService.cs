using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Application.Exceptions;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using FundingPlatform.Domain.ValueObjects;
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

    public async Task<ListApplicationsResult> ListApplicationsAsync(ListApplicationsRequest req, CancellationToken ct = default)
    {
        var page = NormalizePage(req.Page);
        var total = await _queryService.CountApplicationsAsync(req, ct);
        var flatRows = await _queryService.ListApplicationsPageAsync(req, page, PageSize, ct);

        var totals = await _queryService.ApplicationsApprovedTotalsAsync(
            flatRows.Select(r => r.AppId).ToList(), ct);

        var rows = flatRows.Select(p => new ApplicationRowDto(
            AppId: p.AppId,
            ApplicantFullName: p.ApplicantFullName,
            ApplicantLegalId: p.ApplicantLegalId,
            State: p.State,
            CreatedAt: p.CreatedAt,
            SubmittedAt: p.SubmittedAt,
            ResolvedAt: null,
            ItemCount: p.ItemCount,
            TotalApproved: totals.TryGetValue(p.AppId, out var stack) ? stack : Array.Empty<CurrencyAmount>(),
            HasAgreement: p.HasAgreement,
            HasActiveAppeal: p.HasActiveAppeal)).ToList();

        return new ListApplicationsResult(rows, total, req);
    }

    public async IAsyncEnumerable<string> ExportApplicationsCsvAsync(
        ListApplicationsRequest req,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var actual = await _queryService.CountApplicationsAsync(req, ct);
        EnforceCsvRowBoundOrThrow(actual);

        yield return CsvLine(
            "App Id", "Applicant Name", "Applicant Legal Id", "State",
            "Created", "Submitted", "Resolved", "Item Count",
            "Approved Amount", "Currency", "Has Agreement", "Has Active Appeal");

        const int batchSize = 200;
        var skip = 0;
        while (skip < actual)
        {
            var batch = await _queryService.ListApplicationsPageAsync(req, (skip / batchSize) + 1, batchSize, ct);
            if (batch.Count == 0) break;

            var totals = await _queryService.ApplicationsApprovedTotalsAsync(
                batch.Select(b => b.AppId).ToList(), ct);

            foreach (var p in batch)
            {
                var stack = totals.TryGetValue(p.AppId, out var s) ? s : Array.Empty<CurrencyAmount>();
                if (stack.Count == 0)
                {
                    yield return CsvLine(
                        p.AppId.ToString(CultureInfo.InvariantCulture),
                        p.ApplicantFullName,
                        p.ApplicantLegalId,
                        p.State.ToString(),
                        p.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                        p.SubmittedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
                        string.Empty,
                        p.ItemCount.ToString(CultureInfo.InvariantCulture),
                        string.Empty,
                        string.Empty,
                        p.HasAgreement ? "true" : "false",
                        p.HasActiveAppeal ? "true" : "false");
                }
                else
                {
                    foreach (var ca in stack)
                    {
                        yield return CsvLine(
                            p.AppId.ToString(CultureInfo.InvariantCulture),
                            p.ApplicantFullName,
                            p.ApplicantLegalId,
                            p.State.ToString(),
                            p.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                            p.SubmittedAt?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty,
                            string.Empty,
                            p.ItemCount.ToString(CultureInfo.InvariantCulture),
                            ca.Amount.ToString(CultureInfo.InvariantCulture),
                            ca.Currency,
                            p.HasAgreement ? "true" : "false",
                            p.HasActiveAppeal ? "true" : "false");
                    }
                }
            }

            skip += batch.Count;
        }
    }

    public async Task<ListApplicantsResult> ListApplicantsAsync(ListApplicantsRequest req, CancellationToken ct = default)
    {
        var page = NormalizePage(req.Page);
        var total = await _queryService.CountApplicantsAsync(req, ct);
        var flat = await _queryService.ListApplicantsPageAsync(req, page, PageSize, ct);
        var ids = flat.Select(p => p.ApplicantId).ToList();

        var approved = await _queryService.ApplicantsApprovedTotalsAsync(ids, ct);
        var executed = await _queryService.ApplicantsExecutedTotalsAsync(ids, ct);

        var rows = flat.Select(p => new ApplicantRowDto(
            FullName: p.FullName,
            LegalId: p.LegalId,
            Email: p.Email,
            TotalApps: p.TotalApps,
            ResolvedCount: p.ResolvedCount,
            ResponseFinalizedCount: p.ResponseFinalizedCount,
            AgreementExecutedCount: p.AgreementExecutedCount,
            ApprovalRate: p.TotalItems == 0 ? null : (decimal?)((decimal)p.ApprovedItems / p.TotalItems),
            TotalApproved: approved.TryGetValue(p.ApplicantId, out var a) ? a : Array.Empty<CurrencyAmount>(),
            TotalExecuted: executed.TryGetValue(p.ApplicantId, out var e) ? e : Array.Empty<CurrencyAmount>(),
            LastActivity: p.LastActivity)).ToList();

        return new ListApplicantsResult(rows, total, req);
    }

    public Task<ListFundedItemsResult> ListFundedItemsAsync(ListFundedItemsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US5.");

    public Task<ListAgingApplicationsResult> ListAgingApplicationsAsync(ListAgingApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US6.");

    public async IAsyncEnumerable<string> ExportApplicantsCsvAsync(
        ListApplicantsRequest req,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var actual = await _queryService.CountApplicantsAsync(req, ct);
        EnforceCsvRowBoundOrThrow(actual);

        yield return CsvLine(
            "Full Name", "Legal Id", "Email",
            "Total Apps", "Resolved Count", "Response Finalized Count", "Agreement Executed Count",
            "Approval Rate", "Approved Amount", "Executed Amount", "Currency", "Last Activity");

        const int batchSize = 200;
        var skip = 0;
        while (skip < actual)
        {
            var batch = await _queryService.ListApplicantsPageAsync(req, (skip / batchSize) + 1, batchSize, ct);
            if (batch.Count == 0) break;
            var ids = batch.Select(b => b.ApplicantId).ToList();
            var approved = await _queryService.ApplicantsApprovedTotalsAsync(ids, ct);
            var executed = await _queryService.ApplicantsExecutedTotalsAsync(ids, ct);

            foreach (var p in batch)
            {
                var aStack = approved.TryGetValue(p.ApplicantId, out var a) ? a : Array.Empty<CurrencyAmount>();
                var eStack = executed.TryGetValue(p.ApplicantId, out var e) ? e : Array.Empty<CurrencyAmount>();
                var currencies = aStack.Select(x => x.Currency)
                    .Concat(eStack.Select(x => x.Currency))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();

                if (currencies.Count == 0)
                {
                    yield return ApplicantRowLine(p, "", "", "");
                    continue;
                }
                foreach (var currency in currencies)
                {
                    var aAmt = aStack.FirstOrDefault(x => x.Currency == currency)?.Amount ?? 0m;
                    var eAmt = eStack.FirstOrDefault(x => x.Currency == currency)?.Amount ?? 0m;
                    yield return ApplicantRowLine(p, currency,
                        aAmt.ToString(CultureInfo.InvariantCulture),
                        eAmt.ToString(CultureInfo.InvariantCulture));
                }
            }

            skip += batch.Count;
        }
    }

    private static string ApplicantRowLine(ApplicantProjection p, string currency, string approvedAmount, string executedAmount)
    {
        var rate = p.TotalItems == 0 ? string.Empty
                                     : ((decimal)p.ApprovedItems / p.TotalItems).ToString("0.####", CultureInfo.InvariantCulture);
        return CsvLine(
            p.FullName,
            p.LegalId,
            p.Email,
            p.TotalApps.ToString(CultureInfo.InvariantCulture),
            p.ResolvedCount.ToString(CultureInfo.InvariantCulture),
            p.ResponseFinalizedCount.ToString(CultureInfo.InvariantCulture),
            p.AgreementExecutedCount.ToString(CultureInfo.InvariantCulture),
            rate,
            approvedAmount,
            executedAmount,
            currency,
            p.LastActivity?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty);
    }

    public IAsyncEnumerable<string> ExportFundedItemsCsvAsync(ListFundedItemsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US5.");

    public IAsyncEnumerable<string> ExportAgingApplicationsCsvAsync(ListAgingApplicationsRequest req, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in user-story phase US6.");

    private void EnforceCsvRowBoundOrThrow(int actualCount)
    {
        var limit = _options.Value.CsvRowLimit;
        if (actualCount > limit)
        {
            throw new CsvRowBoundExceededException(limit, actualCount);
        }
    }

    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static string CsvLine(params string[] fields)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(EscapeCsv(fields[i]));
        }
        sb.Append('\n');
        return sb.ToString();
    }

    private static string EscapeCsv(string s)
    {
        if (s is null) return string.Empty;
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        if (!needsQuotes) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}

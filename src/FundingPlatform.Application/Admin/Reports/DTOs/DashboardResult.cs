using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.ValueObjects;

namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed record DashboardResult(
    DateRange AppliedRange,
    IReadOnlyList<PipelineCount> Pipeline,
    IReadOnlyList<FinancialKpi> Financial,
    IReadOnlyList<ApplicantKpi> Applicants);

public sealed record PipelineCount(ApplicationState State, int Count);

public sealed record FinancialKpi(string Label, IReadOnlyList<CurrencyAmount> Stack);

public sealed record ApplicantKpi(string Label, int Count);

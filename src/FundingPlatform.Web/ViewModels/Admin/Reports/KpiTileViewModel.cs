using FundingPlatform.Domain.ValueObjects;

namespace FundingPlatform.Web.ViewModels.Admin.Reports;

public sealed record KpiTileViewModel(
    string Label,
    int? NumericValue,
    IReadOnlyList<CurrencyAmount>? Stack);

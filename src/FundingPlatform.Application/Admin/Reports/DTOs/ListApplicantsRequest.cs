namespace FundingPlatform.Application.Admin.Reports.DTOs;

public sealed class ListApplicantsRequest
{
    public string? Search { get; set; }
    public bool? HasExecutedAgreement { get; set; }
    public DateOnly? LastActivityFrom { get; set; }
    public DateOnly? LastActivityTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string Sort { get; set; } = "executed-desc";
}

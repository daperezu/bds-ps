using System.Globalization;
using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FundingPlatform.Web.Helpers;

public static class ReportFilterEncoder
{
    public static IDictionary<string, object?> Encode(ListApplicationsRequest req)
    {
        var dict = new Dictionary<string, object?>();
        if (req.States is { Count: > 0 })
        {
            dict["states"] = req.States.Select(s => s.ToString()).ToArray();
        }
        if (req.From.HasValue) dict["from"] = req.From.Value.ToString("yyyy-MM-dd");
        if (req.To.HasValue) dict["to"] = req.To.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(req.Search)) dict["search"] = req.Search;
        if (req.HasAgreement.HasValue) dict["hasAgreement"] = req.HasAgreement.Value ? "true" : "false";
        if (req.HasActiveAppeal.HasValue) dict["hasActiveAppeal"] = req.HasActiveAppeal.Value ? "true" : "false";
        if (!string.IsNullOrWhiteSpace(req.Sort) && req.Sort != "updated-desc") dict["sort"] = req.Sort;
        if (req.Page > 1) dict["page"] = req.Page.ToString(CultureInfo.InvariantCulture);
        return dict;
    }

    public static IDictionary<string, object?> WithPage(ListApplicationsRequest req, int page)
    {
        var d = Encode(req);
        d["page"] = page.ToString(CultureInfo.InvariantCulture);
        return d;
    }
}

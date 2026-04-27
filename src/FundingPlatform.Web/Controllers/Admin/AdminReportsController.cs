using FundingPlatform.Application.Admin.Reports;
using FundingPlatform.Application.Admin.Reports.DTOs;
using FundingPlatform.Web.ViewModels.Admin.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FundingPlatform.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/Reports")]
public class AdminReportsController : Controller
{
    private readonly IAdminReportsService _reportsService;

    public AdminReportsController(IAdminReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var range = new DateRange(from ?? today.AddDays(-30), to ?? today);
        var result = await _reportsService.GetDashboardAsync(range, ct);

        var pipelineTiles = result.Pipeline
            .Select(p => new KpiTileViewModel(p.State.ToString(), p.Count, null))
            .ToList();

        var financialTiles = result.Financial
            .Select(f => new KpiTileViewModel(f.Label, null, f.Stack))
            .ToList();

        var applicantTiles = result.Applicants
            .Select(a => new KpiTileViewModel(a.Label, a.Count, null))
            .ToList();

        var vm = new DashboardViewModel
        {
            AppliedRange = result.AppliedRange,
            PipelineTiles = pipelineTiles,
            FinancialTiles = financialTiles,
            ApplicantTiles = applicantTiles,
        };

        return View(vm);
    }
}

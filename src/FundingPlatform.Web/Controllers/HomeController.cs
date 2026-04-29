using System.Diagnostics;
using System.Security.Claims;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Application.Services;
using FundingPlatform.Infrastructure.Persistence;
using FundingPlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FundingPlatform.Web.Controllers;

public class HomeController : Controller
{
    private readonly IApplicantDashboardProjection _applicantDashboard;
    private readonly AppDbContext _dbContext;

    public HomeController(
        IApplicantDashboardProjection applicantDashboard,
        AppDbContext dbContext)
    {
        _applicantDashboard = applicantDashboard;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Spec 011 US1 (FR-024) — Applicant role lands on the new dashboard.
        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Applicant")
            && !User.IsInRole("Reviewer") && !User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var applicant = await _dbContext.Applicants.FirstOrDefaultAsync(a => a.UserId == userId, ct);
                if (applicant is not null)
                {
                    var dto = await _applicantDashboard.GetForUserAsync(applicant.Id, applicant.FirstName, ct);
                    return View("ApplicantDashboard", dto);
                }
            }
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

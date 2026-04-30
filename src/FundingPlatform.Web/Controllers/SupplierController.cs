using System.Security.Claims;
using FundingPlatform.Application.Applications.Commands;
using FundingPlatform.Application.Options;
using FundingPlatform.Application.Services;
using FundingPlatform.Infrastructure.Persistence;
using FundingPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FundingPlatform.Web.Controllers;

[Authorize(Roles = "Applicant")]
[Route("Application/{appId}/Item/{itemId}/Supplier")]
public class SupplierController : Controller
{
    private readonly ApplicationService _applicationService;
    private readonly AppDbContext _dbContext;
    private readonly IOptions<AdminReportsOptions> _adminReportsOptions;

    public SupplierController(
        ApplicationService applicationService,
        AppDbContext dbContext,
        IOptions<AdminReportsOptions> adminReportsOptions)
    {
        _applicationService = applicationService;
        _dbContext = dbContext;
        _adminReportsOptions = adminReportsOptions;
    }

    [HttpGet("Add")]
    public async Task<IActionResult> Add(int appId, int itemId)
    {
        await VerifyOwnershipAsync(appId);

        var viewModel = new AddSupplierViewModel
        {
            ApplicationId = appId,
            ItemId = itemId,
            Currency = (_adminReportsOptions.Value.DefaultCurrency ?? string.Empty).ToUpperInvariant(),
            ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3))
        };

        return View(viewModel);
    }

    [HttpPost("Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int appId, int itemId, AddSupplierViewModel model)
    {
        await VerifyOwnershipAsync(appId);

        if (!ModelState.IsValid)
        {
            model.ApplicationId = appId;
            model.ItemId = itemId;
            return View(model);
        }

        if (model.QuotationFile is null || model.QuotationFile.Length == 0)
        {
            ModelState.AddModelError(nameof(model.QuotationFile), "Se requiere el archivo de la cotización.");
            model.ApplicationId = appId;
            model.ItemId = itemId;
            return View(model);
        }

        try
        {
            var command = new AddSupplierQuotationCommand
            {
                ApplicationId = appId,
                ItemId = itemId,
                SupplierLegalId = model.SupplierLegalId,
                SupplierName = model.SupplierName,
                ContactName = model.ContactName,
                Email = model.Email,
                Phone = model.Phone,
                Location = model.Location,
                HasElectronicInvoice = model.HasElectronicInvoice,
                ShippingDetails = model.ShippingDetails,
                WarrantyInfo = model.WarrantyInfo,
                IsCompliantCCSS = model.IsCompliantCCSS,
                IsCompliantHacienda = model.IsCompliantHacienda,
                IsCompliantSICOP = model.IsCompliantSICOP,
                Price = model.Price,
                Currency = model.Currency,
                ValidUntil = model.ValidUntil,
                FileName = model.QuotationFile.FileName,
                FileContentType = model.QuotationFile.ContentType,
                FileSize = model.QuotationFile.Length
            };

            using var stream = model.QuotationFile.OpenReadStream();
            await _applicationService.AddSupplierQuotationAsync(command, stream);

            TempData["SuccessMessage"] = "Proveedor y cotización agregados con éxito.";
            return RedirectToAction("Details", "Application", new { id = appId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.ApplicationId = appId;
            model.ItemId = itemId;
            return View(model);
        }
    }

    private async Task<int> GetCurrentApplicantIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var applicant = await _dbContext.Applicants
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return applicant?.Id ?? throw new InvalidOperationException("Applicant not found for current user.");
    }

    private async Task VerifyOwnershipAsync(int appId)
    {
        var applicantId = await GetCurrentApplicantIdAsync();
        var application = await _applicationService.GetApplicationAsync(appId);

        if (application is null || application.ApplicantId != applicantId)
        {
            throw new UnauthorizedAccessException("You do not own this application.");
        }
    }
}

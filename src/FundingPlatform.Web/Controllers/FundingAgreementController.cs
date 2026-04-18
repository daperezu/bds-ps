using System.Security.Claims;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Application.FundingAgreements.Commands;
using FundingPlatform.Application.FundingAgreements.Queries;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using FundingPlatform.Application.Services;
using FundingPlatform.Domain.Interfaces;
using FundingPlatform.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Web.Controllers;

[Authorize]
[Route("Applications/{applicationId:int}/FundingAgreement")]
public class FundingAgreementController : Controller
{
    private readonly FundingAgreementService _service;
    private readonly IFundingAgreementHtmlRenderer _htmlRenderer;
    private readonly IFundingAgreementPdfRenderer _pdfRenderer;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IOptions<FunderOptions> _funderOptions;
    private readonly IOptions<FundingAgreementOptions> _agreementOptions;
    private readonly ILogger<FundingAgreementController> _logger;

    public FundingAgreementController(
        FundingAgreementService service,
        IFundingAgreementHtmlRenderer htmlRenderer,
        IFundingAgreementPdfRenderer pdfRenderer,
        IFileStorageService fileStorage,
        UserManager<IdentityUser> userManager,
        IOptions<FunderOptions> funderOptions,
        IOptions<FundingAgreementOptions> agreementOptions,
        ILogger<FundingAgreementController> logger)
    {
        _service = service;
        _htmlRenderer = htmlRenderer;
        _pdfRenderer = pdfRenderer;
        _fileStorage = fileStorage;
        _userManager = userManager;
        _funderOptions = funderOptions;
        _agreementOptions = agreementOptions;
        _logger = logger;
    }

    [HttpGet("Panel")]
    public async Task<IActionResult> Panel(int applicationId)
    {
        var query = await BuildPanelQueryAsync(applicationId);
        var result = await _service.GetPanelAsync(query);

        if (!result.Authorized || result.Panel is null)
        {
            LogUnauthorized(applicationId, "Panel", "access-denied-or-missing");
            return NotFound();
        }

        var viewModel = MapToViewModel(result.Panel);
        return PartialView("~/Views/Applications/_FundingAgreementPanel.cshtml", viewModel);
    }

    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdministrator = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        var application = await _service.LoadForGenerationAsync(applicationId);
        if (application is null)
        {
            LogUnauthorized(applicationId, "Generate", "application-missing");
            return NotFound();
        }

        var canUserAccess = application.CanUserAccessFundingAgreement(
            applicantUserId: userId,
            isAdministrator: isAdministrator,
            isReviewerAssignedToThisApplication: isReviewer);

        if (!canUserAccess)
        {
            LogUnauthorized(applicationId, "Generate", "access-denied");
            return NotFound();
        }

        if (!application.CanUserGenerateFundingAgreement(
                isAdministrator: isAdministrator,
                isReviewerAssignedToThisApplication: isReviewer))
        {
            LogUnauthorized(applicationId, "Generate", "role-forbidden");
            return StatusCode(403);
        }

        if (!application.CanGenerateFundingAgreement(out var precondErrors))
        {
            TempData["FundingAgreementError"] = precondErrors.FirstOrDefault()
                ?? "Funding agreement preconditions are not met.";
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        var documentModel = await BuildDocumentViewModelAsync(application);
        var html = await _htmlRenderer.RenderAsync(
            "~/Views/FundingAgreement/Document.cshtml",
            documentModel);

        byte[] pdfBytes;
        try
        {
            pdfBytes = await _pdfRenderer.RenderAsync(html, baseUrl: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Funding agreement PDF rendering failed. applicationId={ApplicationId}", applicationId);
            TempData["FundingAgreementError"] =
                "The agreement could not be generated. Please try again or contact support.";
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        var fileName = $"FundingAgreement-{application.Id}.pdf";
        var priorStoragePath = application.FundingAgreement?.StoragePath;

        string storagePath;
        using (var pdfStream = new MemoryStream(pdfBytes))
        {
            storagePath = await _fileStorage.SaveFileAsync(pdfStream, fileName, "application/pdf");
        }

        var persist = await _service.PersistGenerationAsync(
            application, userId, fileName, pdfBytes.LongLength, storagePath);

        if (!persist.Success)
        {
            try { await _fileStorage.DeleteFileAsync(storagePath); }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "Failed to clean up orphaned PDF after persistence failure. storagePath={StoragePath}",
                    storagePath);
            }

            if (persist.ConflictDetected)
            {
                TempData["FundingAgreementError"] = persist.Errors.FirstOrDefault();
                return StatusCode(409);
            }

            TempData["FundingAgreementError"] = persist.Errors.FirstOrDefault()
                ?? "Funding agreement generation failed.";
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        if (!string.IsNullOrWhiteSpace(priorStoragePath))
        {
            try { await _fileStorage.DeleteFileAsync(priorStoragePath); }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx,
                    "Failed to delete prior funding agreement file. storagePath={StoragePath}",
                    priorStoragePath);
            }
        }

        TempData["FundingAgreementSuccess"] = "Funding agreement generated.";
        return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
    }

    [HttpGet("Download")]
    public async Task<IActionResult> Download(int applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdministrator = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        var application = await _service.LoadForGenerationAsync(applicationId);
        if (application is null)
        {
            LogUnauthorized(applicationId, "Download", "application-missing");
            return NotFound();
        }

        if (!application.CanUserAccessFundingAgreement(
                applicantUserId: userId,
                isAdministrator: isAdministrator,
                isReviewerAssignedToThisApplication: isReviewer))
        {
            LogUnauthorized(applicationId, "Download", "access-denied");
            return NotFound();
        }

        var agreement = application.FundingAgreement;
        if (agreement is null)
        {
            LogUnauthorized(applicationId, "Download", "agreement-missing");
            return NotFound();
        }

        var stream = await _fileStorage.GetFileAsync(agreement.StoragePath);
        Response.Headers.CacheControl = "private, no-cache";
        return File(stream, agreement.ContentType,
            fileDownloadName: $"FundingAgreement-{application.Id}.pdf");
    }

    [HttpGet("")]
    public async Task<IActionResult> Details(int applicationId)
    {
        var query = await BuildPanelQueryAsync(applicationId);
        var result = await _service.GetPanelAsync(query);

        if (!result.Authorized || result.Panel is null)
        {
            LogUnauthorized(applicationId, "Details", "access-denied-or-missing");
            return NotFound();
        }

        var viewModel = MapToViewModel(result.Panel);
        return View(viewModel);
    }

    private async Task<GetFundingAgreementPanelQuery> BuildPanelQueryAsync(int applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdministrator = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        return new GetFundingAgreementPanelQuery(
            ApplicationId: applicationId,
            UserId: userId,
            IsAdministrator: isAdministrator,
            IsReviewerAssigned: isReviewer);
    }

    private FundingAgreementPanelViewModel MapToViewModel(FundingAgreementPanelDto dto)
    {
        string? downloadUrl = null;
        if (dto.AgreementExists)
        {
            downloadUrl = Url.RouteUrl(new
            {
                controller = "FundingAgreement",
                action = "Download",
                applicationId = dto.ApplicationId
            });
        }

        return new FundingAgreementPanelViewModel
        {
            ApplicationId = dto.ApplicationId,
            AgreementExists = dto.AgreementExists,
            AgreementDownloadUrl = downloadUrl,
            CanGenerate = dto.CanGenerate,
            CanRegenerate = dto.CanRegenerate,
            DisabledReason = dto.DisabledReason,
            GeneratedAtUtc = dto.GeneratedAtUtc,
            GeneratedByDisplayName = dto.GeneratedByDisplayName,
            ShowActions = User.IsInRole("Admin") || User.IsInRole("Reviewer")
        };
    }

    private async Task<FundingAgreementDocumentViewModel> BuildDocumentViewModelAsync(AppEntity application)
    {
        var options = _agreementOptions.Value;
        var funder = _funderOptions.Value;

        var applicant = application.Applicant;
        var items = application.Items
            .Where(i => i.ReviewStatus == Domain.Enums.ItemReviewStatus.Approved
                && i.SelectedSupplierId is not null)
            .ToList();

        var latestResponse = application.ApplicantResponses
            .OrderByDescending(r => r.CycleNumber)
            .FirstOrDefault();

        var acceptedItemIds = latestResponse?.ItemResponses
            .Where(ir => ir.Decision == Domain.Enums.ItemResponseDecision.Accept)
            .Select(ir => ir.ItemId)
            .ToHashSet() ?? new HashSet<int>();

        var rows = new List<FundingAgreementItemRowDto>();
        decimal total = 0m;

        foreach (var item in items.Where(i => acceptedItemIds.Contains(i.Id)))
        {
            var quotation = item.Quotations
                .FirstOrDefault(q => q.SupplierId == item.SelectedSupplierId);
            if (quotation is null) continue;

            var supplierName = quotation.Supplier?.Name ?? "(Supplier)";
            var price = quotation.Price;

            rows.Add(new FundingAgreementItemRowDto(
                ItemId: item.Id,
                ProductName: item.ProductName,
                CategoryName: item.Category?.Name ?? string.Empty,
                SupplierName: supplierName,
                UnitPrice: price,
                LineTotal: price));

            total += price;
        }

        var applicantFullName = applicant is null
            ? string.Empty
            : $"{applicant.FirstName} {applicant.LastName}".Trim();

        return new FundingAgreementDocumentViewModel
        {
            AgreementReference = application.Id.ToString(),
            GeneratedAtUtc = DateTime.UtcNow,
            Funder = funder,
            ApplicantLegalName = applicantFullName,
            ApplicantLegalId = applicant?.LegalId ?? string.Empty,
            ApplicantEmail = applicant?.Email ?? string.Empty,
            ApplicantPhone = applicant?.Phone,
            LocaleCode = string.IsNullOrWhiteSpace(options.LocaleCode) ? "es-CO" : options.LocaleCode,
            CurrencyIsoCode = string.IsNullOrWhiteSpace(options.CurrencyIsoCode) ? "COP" : options.CurrencyIsoCode,
            Items = rows,
            TotalAmount = total
        };
    }

    private void LogUnauthorized(int applicationId, string action, string reasonCode)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "(anonymous)";
        _logger.LogInformation(
            "Funding agreement authorization rejected. applicationId={ApplicationId} userId={UserId} action={Action} reasonCode={ReasonCode}",
            applicationId, userId, action, reasonCode);
    }
}

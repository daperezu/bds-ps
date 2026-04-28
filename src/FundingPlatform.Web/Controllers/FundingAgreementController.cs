using System.Security.Claims;
using System.Text.Json;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Application.FundingAgreements.Commands;
using FundingPlatform.Application.FundingAgreements.Queries;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using FundingPlatform.Application.Services;
using FundingPlatform.Application.SignedUploads.Commands;
using FundingPlatform.Application.SignedUploads.Queries;
using FundingPlatform.Domain.Entities;
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
    private readonly SignedUploadService _signedUploadService;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IFundingAgreementHtmlRenderer _htmlRenderer;
    private readonly IFundingAgreementPdfRenderer _pdfRenderer;
    private readonly IFileStorageService _fileStorage;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<FunderOptions> _funderOptions;
    private readonly IOptions<FundingAgreementOptions> _agreementOptions;
    private readonly ILogger<FundingAgreementController> _logger;

    public FundingAgreementController(
        FundingAgreementService service,
        SignedUploadService signedUploadService,
        IApplicationRepository applicationRepository,
        IFundingAgreementHtmlRenderer htmlRenderer,
        IFundingAgreementPdfRenderer pdfRenderer,
        IFileStorageService fileStorage,
        UserManager<ApplicationUser> userManager,
        IOptions<FunderOptions> funderOptions,
        IOptions<FundingAgreementOptions> agreementOptions,
        ILogger<FundingAgreementController> logger)
    {
        _service = service;
        _signedUploadService = signedUploadService;
        _applicationRepository = applicationRepository;
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
        var viewModel = await BuildPanelViewModelAsync(applicationId);
        if (viewModel is null)
        {
            LogUnauthorized(applicationId, "Panel", "access-denied-or-missing");
            return NotFound();
        }

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

        var isRegeneration = application.FundingAgreement is not null;
        if (isRegeneration)
        {
            if (!application.CanRegenerateFundingAgreement(out var regenErrors))
            {
                var reason = regenErrors.FirstOrDefault() ?? "Regeneration preconditions are not met.";
                application.AddVersionHistory(new VersionHistory(
                    userId,
                    SigningAuditActions.FundingAgreementRegenerationBlocked,
                    JsonSerializer.Serialize(new Dictionary<string, object?>
                    {
                        ["reason"] = reason,
                        ["pendingOrTerminalUploadId"] = application.FundingAgreement!.SignedUploads
                            .OrderByDescending(u => u.UploadedAtUtc)
                            .Select(u => (int?)u.Id)
                            .FirstOrDefault()
                    })));

                await _applicationRepository.UpdateAsync(application);
                await _applicationRepository.SaveChangesAsync();

                TempData["FundingAgreementError"] = reason;
                return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
            }
        }
        else if (!application.CanGenerateFundingAgreement(out var precondErrors))
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

        application.AddVersionHistory(new VersionHistory(
            userId,
            SigningAuditActions.AgreementDownloaded,
            JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["generatedVersion"] = agreement.GeneratedVersion
            })));

        await _applicationRepository.UpdateAsync(application);
        await _applicationRepository.SaveChangesAsync();

        var stream = await _fileStorage.GetFileAsync(agreement.StoragePath);
        Response.Headers.CacheControl = "private, no-cache";
        return File(stream, agreement.ContentType,
            fileDownloadName: $"FundingAgreement-{application.Id}.pdf");
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 50L * 1024 * 1024)]
    public async Task<IActionResult> Upload(int applicationId, UploadSignedAgreementViewModel model)
    {
        if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
        {
            TempData["FundingAgreementError"] = "A signed PDF file is required.";
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await using var stream = model.File.OpenReadStream();
        var command = new UploadSignedAgreementCommand(
            ApplicationId: applicationId,
            UserId: userId,
            GeneratedVersion: model.GeneratedVersion,
            FileName: model.File.FileName,
            ContentType: model.File.ContentType ?? "",
            Size: model.File.Length,
            Content: stream);

        var result = await _signedUploadService.UploadAsync(command);

        return RenderSignedUploadRedirect(applicationId, result,
            successMessage: "Signed agreement uploaded. Awaiting reviewer decision.");
    }

    [HttpPost("ReplaceUpload")]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 50L * 1024 * 1024)]
    public async Task<IActionResult> ReplaceUpload(
        int applicationId, int signedUploadId, UploadSignedAgreementViewModel model)
    {
        if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
        {
            TempData["FundingAgreementError"] = "A signed PDF file is required.";
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        await using var stream = model.File.OpenReadStream();
        var command = new ReplaceSignedUploadCommand(
            ApplicationId: applicationId,
            UserId: userId,
            SignedUploadId: signedUploadId,
            GeneratedVersion: model.GeneratedVersion,
            FileName: model.File.FileName,
            ContentType: model.File.ContentType ?? "",
            Size: model.File.Length,
            Content: stream);

        var result = await _signedUploadService.ReplaceAsync(command);
        return RenderSignedUploadRedirect(applicationId, result,
            successMessage: "Signed agreement replaced.");
    }

    [HttpPost("WithdrawUpload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawUpload(int applicationId, int signedUploadId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var command = new WithdrawSignedUploadCommand(applicationId, userId, signedUploadId);
        var result = await _signedUploadService.WithdrawAsync(command);

        return RenderSignedUploadRedirect(applicationId, result,
            successMessage: "Signed upload withdrawn.");
    }

    [HttpPost("Approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int applicationId, int signedUploadId, string? comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        if (!isAdmin && !isReviewer)
        {
            LogUnauthorized(applicationId, "Approve", "role-forbidden");
            return NotFound();
        }

        var command = new ApproveSignedUploadCommand(
            ApplicationId: applicationId,
            ReviewerUserId: userId,
            IsAdministrator: isAdmin,
            IsReviewerAssigned: isReviewer,
            SignedUploadId: signedUploadId,
            Comment: comment);

        var result = await _signedUploadService.ApproveAsync(command);
        return RenderSignedUploadRedirect(applicationId, result,
            successMessage: "Agreement executed.");
    }

    [HttpPost("Reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int applicationId, int signedUploadId, string? comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        if (!isAdmin && !isReviewer)
        {
            LogUnauthorized(applicationId, "Reject", "role-forbidden");
            return NotFound();
        }

        var command = new RejectSignedUploadCommand(
            ApplicationId: applicationId,
            ReviewerUserId: userId,
            IsAdministrator: isAdmin,
            IsReviewerAssigned: isReviewer,
            SignedUploadId: signedUploadId,
            Comment: comment ?? "");

        var result = await _signedUploadService.RejectAsync(command);
        return RenderSignedUploadRedirect(applicationId, result,
            successMessage: "Upload rejected; the applicant can submit a new one.");
    }

    [HttpGet("DownloadSigned/{signedUploadId:int}")]
    public async Task<IActionResult> DownloadSigned(int applicationId, int signedUploadId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        var query = new GetSignedAgreementDownloadQuery(
            ApplicationId: applicationId,
            SignedUploadId: signedUploadId,
            UserId: userId,
            IsAdministrator: isAdmin,
            IsReviewerAssigned: isReviewer);

        var result = await _signedUploadService.GetDownloadAsync(query);
        if (!result.Authorized || result.Content is null)
        {
            LogUnauthorized(applicationId, "DownloadSigned", "access-denied-or-missing");
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-cache";
        return File(result.Content, result.ContentType ?? "application/pdf",
            fileDownloadName: result.FileName ?? $"SignedAgreement-{applicationId}.pdf");
    }

    [HttpGet("")]
    public async Task<IActionResult> Details(int applicationId)
    {
        var viewModel = await BuildPanelViewModelAsync(applicationId);
        if (viewModel is null)
        {
            LogUnauthorized(applicationId, "Details", "access-denied-or-missing");
            return NotFound();
        }
        return View(viewModel);
    }

    private IActionResult RenderSignedUploadRedirect(
        int applicationId, SignedUploadResult result, string successMessage)
    {
        if (result.Success)
        {
            TempData["FundingAgreementSuccess"] = successMessage;
            return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
        }

        if (result.ConflictDetected)
        {
            TempData["FundingAgreementError"] = result.Error;
            return StatusCode(409);
        }

        TempData["FundingAgreementError"] = result.Error ?? "Signed upload failed.";
        if (result.Error == "Not found." || result.Error is null)
        {
            return NotFound();
        }
        return RedirectToRoute(new { controller = "FundingAgreement", action = "Details", applicationId });
    }

    private async Task<SigningStagePanelViewModel?> BuildPanelViewModelAsync(int applicationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdministrator = User.IsInRole("Admin");
        var isReviewer = User.IsInRole("Reviewer");

        var dto = await _signedUploadService.GetPanelAsync(new GetSigningStagePanelQuery(
            ApplicationId: applicationId,
            UserId: userId,
            IsAdministrator: isAdministrator,
            IsReviewerAssigned: isReviewer));

        if (dto is null) return null;

        return MapToViewModel(dto);
    }

    private SigningStagePanelViewModel MapToViewModel(SigningStagePanelDto dto)
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

        string? approvedDownloadUrl = null;
        if (dto.ApprovedSignedUploadId.HasValue)
        {
            approvedDownloadUrl = Url.RouteUrl(new
            {
                controller = "FundingAgreement",
                action = "DownloadSigned",
                applicationId = dto.ApplicationId,
                signedUploadId = dto.ApprovedSignedUploadId.Value
            });
        }

        return new SigningStagePanelViewModel
        {
            ApplicationId = dto.ApplicationId,
            AgreementExists = dto.AgreementExists,
            AgreementDownloadUrl = downloadUrl,
            CanGenerate = dto.CanGenerate,
            CanRegenerate = dto.CanRegenerate,
            DisabledReason = dto.DisabledReason,
            GeneratedAtUtc = dto.GeneratedAtUtc,
            GeneratedByDisplayName = dto.GeneratedByDisplayName,
            GeneratedVersion = dto.GeneratedVersion,
            ShowActions = User.IsInRole("Admin") || User.IsInRole("Reviewer"),
            PendingUpload = dto.PendingUpload,
            LastDecision = dto.LastDecision,
            ApprovedSignedUploadId = dto.ApprovedSignedUploadId,
            ApprovedSignedDownloadUrl = approvedDownloadUrl,
            CanApplicantUpload = dto.CanApplicantUpload,
            CanApplicantReplaceOrWithdraw = dto.CanApplicantReplaceOrWithdraw,
            CanReviewerAct = dto.CanReviewerAct,
            IsExecuted = dto.IsExecuted
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
                LineTotal: price,
                Currency: quotation.Currency));

            total += price;
        }

        var totalsByCurrency = rows
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyTotal(g.Key, g.Sum(r => r.LineTotal)))
            .OrderBy(t => t.Currency)
            .ToList();

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
            TotalAmount = total,
            TotalsByCurrency = totalsByCurrency
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

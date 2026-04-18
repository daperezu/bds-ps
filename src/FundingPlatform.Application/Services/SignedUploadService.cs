using System.Text.Json;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Application.Interfaces;
using FundingPlatform.Application.Options;
using FundingPlatform.Application.SignedUploads.Commands;
using FundingPlatform.Application.SignedUploads.Queries;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Application.Services;

public record SignedUploadResult(
    bool Success,
    string? Error,
    bool ValidationError,
    bool ConflictDetected,
    int? SignedUploadId);

public record SigningInboxResult(
    IReadOnlyList<SigningInboxRowDto> Rows,
    int TotalCount);

public record SignedAgreementStreamResult(
    bool Authorized,
    Stream? Content,
    string? FileName,
    string? ContentType);

public class SignedUploadService
{
    private static readonly byte[] PdfMagicHeader = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // "%PDF-"

    private readonly IApplicationRepository _applicationRepository;
    private readonly ISignedUploadRepository _signedUploadRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IOptions<SignedUploadOptions> _options;
    private readonly ILogger<SignedUploadService> _logger;

    public SignedUploadService(
        IApplicationRepository applicationRepository,
        ISignedUploadRepository signedUploadRepository,
        IFileStorageService fileStorage,
        IOptions<SignedUploadOptions> options,
        ILogger<SignedUploadService> logger)
    {
        _applicationRepository = applicationRepository;
        _signedUploadRepository = signedUploadRepository;
        _fileStorage = fileStorage;
        _options = options;
        _logger = logger;
    }

    public async Task<SigningStagePanelDto?> GetPanelAsync(GetSigningStagePanelQuery query)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(query.ApplicationId);
        if (application is null) return null;

        var canAccess = application.CanUserAccessFundingAgreement(
            applicantUserId: query.UserId,
            isAdministrator: query.IsAdministrator,
            isReviewerAssignedToThisApplication: query.IsReviewerAssigned);

        if (!canAccess) return null;

        var canUserGenerate = application.CanUserGenerateFundingAgreement(
            isAdministrator: query.IsAdministrator,
            isReviewerAssignedToThisApplication: query.IsReviewerAssigned);

        var preconditionsOk = application.CanGenerateFundingAgreement(out var baseErrors);
        var agreement = application.FundingAgreement;
        var agreementExists = agreement is not null;

        var canGenerate = canUserGenerate && preconditionsOk && !agreementExists;
        var canRegenerateErrors = new List<string>();
        var canRegeneratePred = application.CanRegenerateFundingAgreement(out var regenErrors);
        if (!canRegeneratePred) canRegenerateErrors.AddRange(regenErrors);
        var canRegenerate = canUserGenerate && canRegeneratePred && agreementExists;

        var pendingUpload = agreement?.PendingUpload;
        SignedUploadSummaryDto? pendingDto = pendingUpload is null
            ? null
            : new SignedUploadSummaryDto(
                SignedUploadId: pendingUpload.Id,
                UploadedAtUtc: pendingUpload.UploadedAtUtc,
                UploaderUserId: pendingUpload.UploaderUserId,
                UploaderDisplayName: null,
                FileName: pendingUpload.FileName,
                Size: pendingUpload.Size,
                GeneratedVersionAtUpload: pendingUpload.GeneratedVersionAtUpload,
                Status: pendingUpload.Status);

        var lastTerminal = agreement?.SignedUploads
            .Where(u => u.Status != SignedUploadStatus.Pending)
            .OrderByDescending(u => u.UploadedAtUtc)
            .FirstOrDefault(u => u.ReviewDecision is not null);

        SigningReviewDecisionDto? lastDecisionDto = lastTerminal?.ReviewDecision is null
            ? null
            : new SigningReviewDecisionDto(
                Outcome: lastTerminal.ReviewDecision.Outcome,
                Comment: lastTerminal.ReviewDecision.Comment,
                DecidedAtUtc: lastTerminal.ReviewDecision.DecidedAtUtc,
                ReviewerUserId: lastTerminal.ReviewDecision.ReviewerUserId,
                ReviewerDisplayName: null);

        var approvedUpload = agreement?.SignedUploads
            .FirstOrDefault(u => u.Status == SignedUploadStatus.Approved);

        var isApplicantOwner =
            query.UserId is not null &&
            application.Applicant?.UserId == query.UserId;

        var isResponseFinalized = application.State == ApplicationState.ResponseFinalized;
        var isExecuted = application.State == ApplicationState.AgreementExecuted;

        var canApplicantUpload =
            isApplicantOwner && isResponseFinalized && agreementExists && pendingUpload is null;
        var canApplicantReplaceOrWithdraw =
            isApplicantOwner && isResponseFinalized && pendingUpload is not null
            && pendingUpload.UploaderUserId == query.UserId;
        var canReviewerAct =
            application.CanUserReviewSignedUpload(query.IsAdministrator, query.IsReviewerAssigned)
            && isResponseFinalized
            && pendingUpload is not null;

        var disabledReason = !preconditionsOk && !agreementExists
            ? baseErrors.FirstOrDefault()
            : null;
        if (disabledReason is null && agreementExists && !canRegeneratePred)
        {
            disabledReason = canRegenerateErrors.FirstOrDefault();
        }

        return new SigningStagePanelDto(
            ApplicationId: application.Id,
            AgreementExists: agreementExists,
            CanGenerate: canGenerate,
            CanRegenerate: canRegenerate,
            DisabledReason: disabledReason,
            GeneratedAtUtc: agreement?.GeneratedAtUtc,
            GeneratedByUserId: agreement?.GeneratedByUserId,
            GeneratedByDisplayName: agreement?.GeneratedByUserId,
            GeneratedVersion: agreement?.GeneratedVersion ?? 0,
            PendingUpload: pendingDto,
            LastDecision: lastDecisionDto,
            ApprovedSignedUploadId: approvedUpload?.Id,
            CanApplicantUpload: canApplicantUpload,
            CanApplicantReplaceOrWithdraw: canApplicantReplaceOrWithdraw,
            CanReviewerAct: canReviewerAct,
            IsExecuted: isExecuted);
    }

    public async Task<SignedUploadResult> UploadAsync(UploadSignedAgreementCommand command)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null)
            return NotFound();

        if (application.Applicant?.UserId != command.UserId)
            return NotFound();

        if (application.FundingAgreement is null)
            return NotFound();

        var intakeError = ValidateIntake(command.ContentType, command.Size, command.Content);
        if (intakeError is not null)
            return Validation(intakeError);

        if (command.GeneratedVersion != application.FundingAgreement.GeneratedVersion)
            return Validation("Please re-download the latest agreement and re-sign.");

        if (application.FundingAgreement.PendingUpload is not null)
            return Validation("A pending signed upload already exists. Use Replace instead.");

        command.Content.Position = 0;
        var storagePath = await _fileStorage.SaveFileAsync(command.Content, command.FileName, command.ContentType);

        try
        {
            SignedUpload upload;
            try
            {
                upload = application.SubmitSignedUpload(
                    command.UserId,
                    command.GeneratedVersion,
                    command.FileName,
                    command.Size,
                    storagePath);
            }
            catch (InvalidOperationException ex)
            {
                await TryDeleteAsync(storagePath);
                return Validation(ex.Message);
            }

            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                SigningAuditActions.SignedAgreementUploaded,
                SerializeDetails(new Dictionary<string, object?>
                {
                    ["signedUploadId"] = 0, // filled after save if needed
                    ["generatedVersion"] = command.GeneratedVersion,
                    ["fileName"] = command.FileName,
                    ["size"] = command.Size
                })));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Signed agreement uploaded. applicationId={ApplicationId} actingUserId={UserId} signedUploadId={SignedUploadId}",
                application.Id, command.UserId, upload.Id);

            return new SignedUploadResult(true, null, false, false, upload.Id);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await TryDeleteAsync(storagePath);
            return Conflict();
        }
        catch
        {
            await TryDeleteAsync(storagePath);
            throw;
        }
    }

    public async Task<SignedUploadResult> ReplaceAsync(ReplaceSignedUploadCommand command)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return NotFound();
        if (application.Applicant?.UserId != command.UserId) return NotFound();
        if (application.FundingAgreement is null) return NotFound();

        var pending = application.FundingAgreement.PendingUpload;
        if (pending is null)
            return Validation("No pending upload to replace; use Upload.");
        if (pending.Id != command.SignedUploadId)
            return Validation("The specified upload is not the current pending upload.");
        if (pending.UploaderUserId != command.UserId)
            return NotFound();

        var intakeError = ValidateIntake(command.ContentType, command.Size, command.Content);
        if (intakeError is not null)
            return Validation(intakeError);

        if (command.GeneratedVersion != application.FundingAgreement.GeneratedVersion)
            return Validation("Please re-download the latest agreement and re-sign.");

        command.Content.Position = 0;
        var storagePath = await _fileStorage.SaveFileAsync(command.Content, command.FileName, command.ContentType);

        try
        {
            SignedUpload newUpload;
            var supersededId = pending.Id;
            try
            {
                newUpload = application.ReplaceSignedUpload(
                    command.UserId,
                    command.GeneratedVersion,
                    command.FileName,
                    command.Size,
                    storagePath);
            }
            catch (InvalidOperationException ex)
            {
                await TryDeleteAsync(storagePath);
                return Validation(ex.Message);
            }

            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                SigningAuditActions.SignedUploadReplaced,
                SerializeDetails(new Dictionary<string, object?>
                {
                    ["supersededId"] = supersededId,
                    ["newSignedUploadId"] = 0,
                    ["generatedVersion"] = command.GeneratedVersion
                })));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            return new SignedUploadResult(true, null, false, false, newUpload.Id);
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            await TryDeleteAsync(storagePath);
            return Conflict();
        }
        catch
        {
            await TryDeleteAsync(storagePath);
            throw;
        }
    }

    public async Task<SignedUploadResult> WithdrawAsync(WithdrawSignedUploadCommand command)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return NotFound();
        if (application.Applicant?.UserId != command.UserId) return NotFound();
        if (application.FundingAgreement is null) return NotFound();

        var pending = application.FundingAgreement.PendingUpload;
        if (pending is null) return Validation("No pending upload to withdraw.");
        if (pending.Id != command.SignedUploadId) return Validation("Stale pending upload id; please refresh.");
        if (pending.UploaderUserId != command.UserId) return NotFound();

        try
        {
            application.WithdrawSignedUpload(command.UserId);
        }
        catch (InvalidOperationException ex)
        {
            return Validation(ex.Message);
        }

        application.AddVersionHistory(new VersionHistory(
            command.UserId,
            SigningAuditActions.SignedUploadWithdrawn,
            SerializeDetails(new Dictionary<string, object?>
            {
                ["signedUploadId"] = pending.Id
            })));

        try
        {
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            return Conflict();
        }

        return new SignedUploadResult(true, null, false, false, pending.Id);
    }

    public async Task<SignedUploadResult> ApproveAsync(ApproveSignedUploadCommand command)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return NotFound();

        if (!application.CanUserReviewSignedUpload(command.IsAdministrator, command.IsReviewerAssigned))
            return NotFound();

        if (application.FundingAgreement is null) return NotFound();

        var pending = application.FundingAgreement.PendingUpload;
        if (pending is null) return Validation("No pending upload to approve.");
        if (pending.Id != command.SignedUploadId) return Validation("Stale pending upload id; please refresh.");

        try
        {
            application.ApproveSignedUpload(command.ReviewerUserId, command.Comment);
        }
        catch (InvalidOperationException ex)
        {
            return Validation(ex.Message);
        }

        application.AddVersionHistory(new VersionHistory(
            command.ReviewerUserId,
            SigningAuditActions.SignedUploadApproved,
            SerializeDetails(new Dictionary<string, object?>
            {
                ["signedUploadId"] = pending.Id,
                ["comment"] = command.Comment
            })));

        try
        {
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            return Conflict();
        }

        return new SignedUploadResult(true, null, false, false, pending.Id);
    }

    public async Task<SignedUploadResult> RejectAsync(RejectSignedUploadCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Comment))
            return Validation("Rejection comment is required.");

        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return NotFound();

        if (!application.CanUserReviewSignedUpload(command.IsAdministrator, command.IsReviewerAssigned))
            return NotFound();

        if (application.FundingAgreement is null) return NotFound();

        var pending = application.FundingAgreement.PendingUpload;
        if (pending is null) return Validation("No pending upload to reject.");
        if (pending.Id != command.SignedUploadId) return Validation("Stale pending upload id; please refresh.");

        try
        {
            application.RejectSignedUpload(command.ReviewerUserId, command.Comment);
        }
        catch (InvalidOperationException ex)
        {
            return Validation(ex.Message);
        }

        application.AddVersionHistory(new VersionHistory(
            command.ReviewerUserId,
            SigningAuditActions.SignedUploadRejected,
            SerializeDetails(new Dictionary<string, object?>
            {
                ["signedUploadId"] = pending.Id,
                ["comment"] = command.Comment
            })));

        try
        {
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
        }
        catch (Exception ex) when (IsConcurrencyException(ex))
        {
            return Conflict();
        }

        return new SignedUploadResult(true, null, false, false, pending.Id);
    }

    public async Task<SignedAgreementStreamResult> GetDownloadAsync(GetSignedAgreementDownloadQuery query)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(query.ApplicationId);
        if (application is null)
            return new SignedAgreementStreamResult(false, null, null, null);

        if (!application.CanUserAccessFundingAgreement(query.UserId, query.IsAdministrator, query.IsReviewerAssigned))
            return new SignedAgreementStreamResult(false, null, null, null);

        var agreement = application.FundingAgreement;
        if (agreement is null)
            return new SignedAgreementStreamResult(false, null, null, null);

        var upload = agreement.SignedUploads.FirstOrDefault(u => u.Id == query.SignedUploadId);
        if (upload is null)
            return new SignedAgreementStreamResult(false, null, null, null);

        var stream = await _fileStorage.GetFileAsync(upload.StoragePath);
        return new SignedAgreementStreamResult(true, stream, upload.FileName, upload.ContentType);
    }

    public async Task<SigningInboxResult> GetInboxAsync(GetSigningInboxQuery query)
    {
        var (rows, totalCount) = await _signedUploadRepository.GetPendingInboxAsync(
            reviewerUserId: query.CurrentUserId,
            isAdmin: query.IsAdministrator,
            page: query.Page,
            pageSize: query.PageSize);

        return new SigningInboxResult(rows, totalCount);
    }

    private string? ValidateIntake(string contentType, long size, Stream content)
    {
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return "Only application/pdf uploads are accepted.";

        if (size <= 0)
            return "Uploaded file is empty.";

        var maxSize = _options.Value.MaxSizeBytes;
        if (size > maxSize)
            return $"File exceeds maximum size of {maxSize} bytes.";

        if (!content.CanRead)
            return "Unable to read uploaded content.";

        if (content.CanSeek) content.Position = 0;

        Span<byte> header = stackalloc byte[5];
        var readTotal = 0;
        while (readTotal < header.Length)
        {
            var read = content.Read(header.Slice(readTotal));
            if (read <= 0) break;
            readTotal += read;
        }

        if (content.CanSeek) content.Position = 0;

        if (readTotal < PdfMagicHeader.Length)
            return "Uploaded file does not appear to be a PDF.";

        for (var i = 0; i < PdfMagicHeader.Length; i++)
        {
            if (header[i] != PdfMagicHeader[i])
                return "Uploaded file does not appear to be a PDF (missing %PDF- header).";
        }

        return null;
    }

    private async Task TryDeleteAsync(string storagePath)
    {
        try { await _fileStorage.DeleteFileAsync(storagePath); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to clean up signed-upload file after persistence failure. storagePath={StoragePath}",
                storagePath);
        }
    }

    private static bool IsConcurrencyException(Exception ex) =>
        ex.GetType().Name == "DbUpdateConcurrencyException";

    private static string SerializeDetails(IDictionary<string, object?> values) =>
        JsonSerializer.Serialize(values);

    private static SignedUploadResult NotFound() =>
        new(false, "Not found.", false, false, null);

    private static SignedUploadResult Validation(string message) =>
        new(false, message, true, false, null);

    private static SignedUploadResult Conflict() =>
        new(false, "Another action just modified this upload; please refresh.", false, true, null);
}

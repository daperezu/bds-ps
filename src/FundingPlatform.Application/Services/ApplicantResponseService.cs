using FundingPlatform.Application.Applications.Commands;
using FundingPlatform.Application.Applications.Queries;
using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Application.Services;

public class ApplicantResponseService
{
    private const int DefaultMaxAppeals = 1;

    private readonly IApplicationRepository _applicationRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository;
    private readonly ILogger<ApplicantResponseService> _logger;

    public ApplicantResponseService(
        IApplicationRepository applicationRepository,
        ISystemConfigurationRepository systemConfigurationRepository,
        ILogger<ApplicantResponseService> logger)
    {
        _applicationRepository = applicationRepository;
        _systemConfigurationRepository = systemConfigurationRepository;
        _logger = logger;
    }

    public async Task<ApplicantResponseDto?> GetResponseAsync(GetApplicantResponseQuery query, int applicantId)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(query.ApplicationId);
        if (application is null) return null;
        if (application.ApplicantId != applicantId) return null;

        return MapToResponseDto(application);
    }

    public async Task<(ApplicantResponseDto? Result, string? Error)> SubmitResponseAsync(
        SubmitApplicantResponseCommand command,
        int applicantId)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return (null, "Application not found.");
        if (application.ApplicantId != applicantId) return (null, "You do not own this application.");

        try
        {
            application.SubmitResponse(command.ItemDecisions, command.UserId);
            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                "SubmitResponse",
                $"Applicant response submitted (cycle {application.ApplicantResponses.Count})"));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            return (MapToResponseDto(application), null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return (null, "This application has been modified by another user. Please refresh and try again.");
        }
    }

    public async Task<(AppealDto? Result, string? Error)> OpenAppealAsync(
        OpenAppealCommand command,
        int applicantId)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return (null, "Application not found.");
        if (application.ApplicantId != applicantId) return (null, "You do not own this application.");

        var maxAppeals = await GetMaxAppealsAsync();

        try
        {
            var appeal = application.OpenAppeal(command.UserId, maxAppeals);
            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                "OpenAppeal",
                "Applicant opened an appeal"));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            return (MapAppealToDto(appeal, application), null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return (null, "This application has been modified by another user. Please refresh and try again.");
        }
    }

    public async Task<AppealDto?> GetAppealAsync(GetAppealQuery query, int? applicantId)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(query.ApplicationId);
        if (application is null) return null;

        if (!query.IsReviewer)
        {
            if (applicantId is null || application.ApplicantId != applicantId) return null;
        }

        var appeal = application.Appeals
            .OrderByDescending(a => a.OpenedAt)
            .FirstOrDefault();
        if (appeal is null) return null;

        return MapAppealToDto(appeal, application);
    }

    public async Task<(AppealDto? Result, string? Error)> PostMessageAsync(
        PostAppealMessageCommand command,
        int? applicantId,
        bool isReviewer)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return (null, "Application not found.");

        if (!isReviewer)
        {
            if (applicantId is null || application.ApplicantId != applicantId)
                return (null, "You do not have access to this appeal.");
        }

        var appeal = application.Appeals
            .OrderByDescending(a => a.OpenedAt)
            .FirstOrDefault(a => a.Status == AppealStatus.Open);
        if (appeal is null) return (null, "No open appeal to post on.");

        try
        {
            appeal.PostMessage(command.UserId, command.Text);
            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                "PostAppealMessage",
                $"Message posted on appeal {appeal.Id}"));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            return (MapAppealToDto(appeal, application), null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return (null, "This appeal has been modified by another user. Please refresh and try again.");
        }
    }

    public async Task<(AppealDto? Result, string? Error)> ResolveAppealAsync(
        ResolveAppealCommand command)
    {
        var application = await _applicationRepository.GetByIdWithResponseAndAppealsAsync(command.ApplicationId);
        if (application is null) return (null, "Application not found.");

        try
        {
            switch (command.Resolution)
            {
                case AppealResolution.Uphold:
                    application.ResolveAppealAsUphold(command.UserId);
                    break;
                case AppealResolution.GrantReopenToDraft:
                    application.ResolveAppealAsGrantReopenToDraft(command.UserId);
                    break;
                case AppealResolution.GrantReopenToReview:
                    application.ResolveAppealAsGrantReopenToReview(command.UserId);
                    break;
                default:
                    return (null, $"Unknown resolution '{command.Resolution}'.");
            }

            application.AddVersionHistory(new VersionHistory(
                command.UserId,
                "ResolveAppeal",
                $"Appeal resolved as {command.Resolution}"));

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            var appeal = application.Appeals
                .OrderByDescending(a => a.ResolvedAt ?? a.OpenedAt)
                .First();
            return (MapAppealToDto(appeal, application), null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return (null, "This appeal has been modified by another user. Please refresh and try again.");
        }
    }

    private async Task<int> GetMaxAppealsAsync()
    {
        var config = await _systemConfigurationRepository.GetByKeyAsync("MaxAppealsPerApplication");
        if (config is null)
        {
            _logger.LogWarning("SystemConfiguration key 'MaxAppealsPerApplication' not found. Using default value of {Default}.", DefaultMaxAppeals);
            return DefaultMaxAppeals;
        }

        return int.TryParse(config.Value, out var parsed) ? parsed : DefaultMaxAppeals;
    }

    private static ApplicantResponseDto MapToResponseDto(AppEntity application)
    {
        var latestResponse = application.ApplicantResponses
            .OrderByDescending(r => r.CycleNumber)
            .FirstOrDefault();

        var decisionsByItemId = latestResponse is null
            ? new Dictionary<int, ItemResponseDecision>()
            : latestResponse.ItemResponses.ToDictionary(ir => ir.ItemId, ir => ir.Decision);

        var items = application.Items.Select(item =>
        {
            decimal? amount = null;
            string? supplierName = null;
            if (item.SelectedSupplierId is int supplierId)
            {
                var quotation = item.Quotations.FirstOrDefault(q => q.SupplierId == supplierId);
                amount = quotation?.Price;
                supplierName = quotation?.Supplier?.Name;
            }

            var hasDecision = decisionsByItemId.TryGetValue(item.Id, out var decision);

            return new ItemResponseDto(
                item.Id,
                item.ProductName,
                item.ReviewStatus,
                supplierName,
                amount,
                item.ReviewComment,
                hasDecision ? decision : null);
        }).ToList();

        return new ApplicantResponseDto(
            application.Id,
            latestResponse?.CycleNumber,
            latestResponse?.SubmittedAt,
            latestResponse is not null,
            application.State,
            items,
            application.FundingAgreement is not null);
    }

    private static AppealDto MapAppealToDto(Appeal appeal, AppEntity application)
    {
        var applicantUserId = application.Applicant?.UserId;
        var applicantDisplayName = application.Applicant is not null
            ? $"{application.Applicant.FirstName} {application.Applicant.LastName}"
            : "Applicant";

        var messages = appeal.Messages
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m =>
            {
                var isByApplicant = m.AuthorUserId == applicantUserId;
                return new AppealMessageDto(
                    m.Id,
                    m.AuthorUserId,
                    isByApplicant ? applicantDisplayName : "Reviewer",
                    isByApplicant,
                    m.Text,
                    m.CreatedAt);
            }).ToList();

        return new AppealDto(
            appeal.Id,
            appeal.ApplicationId,
            appeal.OpenedAt,
            appeal.OpenedByUserId,
            appeal.Status,
            appeal.Resolution,
            appeal.ResolvedAt,
            appeal.ResolvedByUserId,
            messages);
    }
}

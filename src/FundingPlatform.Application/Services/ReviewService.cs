using FundingPlatform.Application.DTOs;
using FundingPlatform.Domain.Entities;
using FundingPlatform.Domain.Enums;
using FundingPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using AppEntity = FundingPlatform.Domain.Entities.Application;

namespace FundingPlatform.Application.Services;

public class ReviewService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILogger<ReviewService> _logger;

    private const int PageSize = 25;

    public ReviewService(IApplicationRepository applicationRepository, ILogger<ReviewService> logger)
    {
        _applicationRepository = applicationRepository;
        _logger = logger;
    }

    public async Task<(List<ReviewQueueItemDto> Items, int TotalCount)> GetReviewQueueAsync(int page)
    {
        var (applications, totalCount) = await _applicationRepository.GetByStatePagedAsync(
            ApplicationState.Submitted, page, PageSize);

        var items = applications.Select(a => new ReviewQueueItemDto(
            a.Id,
            $"{a.Applicant.FirstName} {a.Applicant.LastName}",
            a.Applicant.PerformanceScore,
            a.SubmittedAt!.Value,
            a.Items.Count)).ToList();

        return (items, totalCount);
    }

    public async Task<ReviewApplicationDto?> GetApplicationForReviewAsync(int applicationId)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId);
        if (application is null)
            return null;

        // Transition to UnderReview if currently Submitted
        if (application.State == ApplicationState.Submitted)
        {
            application.StartReview();
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
        }

        return MapToReviewDto(application);
    }

    public async Task<string?> ReviewItemAsync(int applicationId, int itemId, string decision, string? comment, int? selectedSupplierId, string userId)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId);
        if (application is null) return "Application not found.";
        if (application.State != ApplicationState.UnderReview)
            return "Application is not under review.";

        var item = application.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return "Item not found.";

        try
        {
            switch (decision)
            {
                case "Approve":
                    if (!selectedSupplierId.HasValue)
                        return "A supplier must be selected when approving an item.";
                    item.Approve(selectedSupplierId.Value, comment);
                    break;
                case "Reject":
                    item.Reject(comment);
                    break;
                case "RequestMoreInfo":
                    item.RequestMoreInfo(comment);
                    break;
                default:
                    return $"Invalid decision: {decision}";
            }

            application.AddVersionHistory(new VersionHistory(userId, "ReviewItem",
                $"Item '{item.ProductName}' — {decision}"));
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return "This application has been modified by another user. Please refresh and try again.";
        }
    }

    public async Task<string?> FlagTechnicalEquivalenceAsync(int applicationId, int itemId, bool isNotEquivalent, string userId)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId);
        if (application is null) return "Application not found.";
        if (application.State != ApplicationState.UnderReview)
            return "Application is not under review.";

        var item = application.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return "Item not found.";

        try
        {
            if (isNotEquivalent)
                item.FlagNotEquivalent();
            else
                item.ClearNotEquivalentFlag();

            application.AddVersionHistory(new VersionHistory(userId, "FlagEquivalence",
                $"Item '{item.ProductName}' — {(isNotEquivalent ? "flagged" : "cleared")} technical equivalence"));
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
            return null;
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return "This application has been modified by another user. Please refresh and try again.";
        }
    }

    public async Task<string?> SendBackAsync(int applicationId, string userId)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId);
        if (application is null) return "Application not found.";

        try
        {
            application.SendBack();
            application.AddVersionHistory(new VersionHistory(userId, "SendBack",
                "Application sent back to applicant for more information"));
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return "This application has been modified by another user. Please refresh and try again.";
        }
    }

    public async Task<(string? Error, List<string>? UnresolvedItems)> FinalizeReviewAsync(int applicationId, bool force, string userId)
    {
        var application = await _applicationRepository.GetByIdWithDetailsAsync(applicationId);
        if (application is null) return ("Application not found.", null);

        try
        {
            application.Finalize(force);
            application.AddVersionHistory(new VersionHistory(userId, "Finalize",
                $"Review finalized{(force ? " (force — unresolved items implicitly rejected)" : "")}"));
            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
            return (null, null);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("unresolved"))
        {
            var unresolvedItems = application.Items
                .Where(i => i.ReviewStatus == ItemReviewStatus.Pending
                         || i.ReviewStatus == ItemReviewStatus.NeedsInfo)
                .Select(i => i.ProductName)
                .ToList();
            return (null, unresolvedItems);
        }
        catch (InvalidOperationException ex)
        {
            return (ex.Message, null);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return ("This application has been modified by another user. Please refresh and try again.", null);
        }
    }

    private static ReviewApplicationDto MapToReviewDto(AppEntity application)
    {
        var reviewItems = application.Items.Select(item =>
        {
            var quotations = item.Quotations.ToList();
            int? recommendedSupplierId = ComputeRecommendedSupplierId(quotations);

            return new ReviewItemDto(
                item.Id,
                item.ProductName,
                item.Category?.Name ?? string.Empty,
                item.TechnicalSpecifications,
                item.ReviewStatus,
                item.ReviewComment,
                item.SelectedSupplierId,
                item.IsNotTechnicallyEquivalent,
                quotations.Select(q => new ReviewQuotationDto(
                    q.Id,
                    q.SupplierId,
                    q.Supplier?.Name ?? string.Empty,
                    q.Supplier?.LegalId ?? string.Empty,
                    q.Price,
                    q.ValidUntil,
                    q.Document?.OriginalFileName ?? string.Empty,
                    recommendedSupplierId.HasValue && q.SupplierId == recommendedSupplierId.Value)).ToList(),
                recommendedSupplierId,
                item.Impact?.ImpactTemplate?.Name,
                item.Impact?.ParameterValues.Select(pv => new ImpactParameterDisplayDto(
                    pv.ImpactTemplateParameter?.Name ?? string.Empty,
                    pv.ImpactTemplateParameter?.DisplayLabel ?? string.Empty,
                    pv.Value ?? string.Empty)).ToList() ?? []);
        }).ToList();

        return new ReviewApplicationDto(
            application.Id,
            $"{application.Applicant.FirstName} {application.Applicant.LastName}",
            application.Applicant.PerformanceScore,
            application.State,
            application.SubmittedAt,
            reviewItems);
    }

    private static int? ComputeRecommendedSupplierId(List<Quotation> quotations)
    {
        if (quotations.Count == 0) return null;

        var minPrice = quotations.Min(q => q.Price);
        var cheapest = quotations.Where(q => q.Price == minPrice).ToList();

        return cheapest.Count == 1 ? cheapest[0].SupplierId : null;
    }
}

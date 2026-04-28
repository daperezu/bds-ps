using FundingPlatform.Application.DTOs;

namespace FundingPlatform.Application.Services;

public interface IReviewerCopyProvider
{
    string WelcomeHeadline(string firstName);
    string WelcomeSubhead();
    string FilterLabel(ReviewerFilter filter);
    string EmptyHeadline();
    string EmptyBody();
    string KpiAwaiting();
    string KpiInProgress();
    string KpiAging(int thresholdDays);
    string KpiDecidedThisMonth();
}

public sealed class ReviewerCopyProvider : IReviewerCopyProvider
{
    public string WelcomeHeadline(string firstName) => $"Good to see you, {firstName}.";
    public string WelcomeSubhead() => "Here's what's on your plate today.";

    public string FilterLabel(ReviewerFilter filter) => filter switch
    {
        ReviewerFilter.All        => "All",
        ReviewerFilter.AwaitingMe => "Awaiting me",
        ReviewerFilter.Aging      => "Aging",
        ReviewerFilter.SentBack   => "Sent back",
        ReviewerFilter.Appealing  => "Appealing",
        _                         => "All",
    };

    public string EmptyHeadline() => "All clear";
    public string EmptyBody()     => "Nothing's awaiting your review.";

    public string KpiAwaiting()              => "Awaiting your review";
    public string KpiInProgress()            => "In progress";
    public string KpiAging(int days)         => $"Aging > {days} days";
    public string KpiDecidedThisMonth()      => "Decided this month";
}

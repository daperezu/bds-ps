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
    public string WelcomeHeadline(string firstName) => $"Buen día, {firstName}.";
    public string WelcomeSubhead() => "Esto es lo que tiene en su agenda hoy.";

    public string FilterLabel(ReviewerFilter filter) => filter switch
    {
        ReviewerFilter.All        => "Todas",
        ReviewerFilter.AwaitingMe => "Pendientes para mí",
        ReviewerFilter.Aging      => "Antiguas",
        ReviewerFilter.SentBack   => "Devueltas",
        ReviewerFilter.Appealing  => "En apelación",
        _                         => "Todas",
    };

    public string EmptyHeadline() => "Todo en orden";
    public string EmptyBody()     => "No tiene revisiones pendientes.";

    public string KpiAwaiting()              => "Pendientes de su revisión";
    public string KpiInProgress()            => "En proceso";
    public string KpiAging(int days)         => $"Antiguas > {days} días";
    public string KpiDecidedThisMonth()      => "Decididas este mes";
}

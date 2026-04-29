using FundingPlatform.Application.DTOs;

namespace FundingPlatform.Application.Services;

public interface ICeremonyCopyProvider
{
    string Headline(SigningCeremonyVariant variant);
    string Subhead(SigningCeremonyVariant variant, DateOnly disbursementDate);
    bool ShouldFireConfetti(SigningCeremonyVariant variant);
    string AriaLiveAnnouncement(SigningCeremonyVariant variant);
    string PrimaryCtaLabel(SigningCeremonyVariant variant);
    string SecondaryCtaLabel();
}

public sealed class CeremonyCopyProvider : ICeremonyCopyProvider
{
    public string Headline(SigningCeremonyVariant variant) => variant switch
    {
        SigningCeremonyVariant.ApplicantOnlySigned       => "You're signed.",
        SigningCeremonyVariant.FunderOnlySigned          => "Funder signature recorded.",
        SigningCeremonyVariant.BothCompleteApplicantLast => "Your funding is locked in.",
        SigningCeremonyVariant.BothCompleteFunderLast    => "Your funding is locked in.",
        _                                                => "You're signed.",
    };

    public string Subhead(SigningCeremonyVariant variant, DateOnly date) => variant switch
    {
        SigningCeremonyVariant.ApplicantOnlySigned => "We're waiting on the funder. We'll email you when it's complete.",
        SigningCeremonyVariant.FunderOnlySigned    => "The applicant has been notified.",
        SigningCeremonyVariant.BothCompleteApplicantLast => $"Funds will be transferred by {date:MMM d, yyyy}.",
        SigningCeremonyVariant.BothCompleteFunderLast    => $"Funds will be transferred by {date:MMM d, yyyy}.",
        _ => string.Empty,
    };

    public bool ShouldFireConfetti(SigningCeremonyVariant variant)
        => variant is SigningCeremonyVariant.BothCompleteApplicantLast
                   or SigningCeremonyVariant.BothCompleteFunderLast;

    public string AriaLiveAnnouncement(SigningCeremonyVariant variant) => variant switch
    {
        SigningCeremonyVariant.BothCompleteApplicantLast => "Your funding agreement is signed.",
        SigningCeremonyVariant.BothCompleteFunderLast    => "Your funding agreement is signed.",
        SigningCeremonyVariant.ApplicantOnlySigned       => "Your signature was recorded.",
        SigningCeremonyVariant.FunderOnlySigned          => "Funder signature was recorded.",
        _                                                => "Signing event recorded.",
    };

    public string PrimaryCtaLabel(SigningCeremonyVariant variant) => "View funding details";
    public string SecondaryCtaLabel() => "Back to dashboard";
}

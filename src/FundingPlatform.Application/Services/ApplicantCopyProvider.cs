namespace FundingPlatform.Application.Services;

/// <summary>
/// Spec 011 — applicant-facing voice-guide-compliant copy strings.
/// Centralized so a single voice-guide pass lints them in one place.
/// See specs/011-warm-modern-facelift/BRAND-VOICE.md.
/// </summary>
public interface IApplicantCopyProvider
{
    string WelcomeHeadline(string firstName);
    string WelcomeSubhead();
    string AwaitingActionDraft(string projectName);
    string AwaitingActionSentBack(string projectName);
    string AwaitingActionAgreement(string projectName);
    string EmptyHeroHeadline();
    string EmptyHeroSubhead();
    string EmptyCtaLabel();
    string ResourcesHowFundingWorks();
    string ResourcesSubmissionTips();
    string ResourcesGetHelp();
    string TrustHowLongTitle();
    string TrustHowLongBody();
    string TrustWhatYouNeedTitle();
    string TrustWhatYouNeedBody();
    string TrustHowDecisionsTitle();
    string TrustHowDecisionsBody();
}

public sealed class ApplicantCopyProvider : IApplicantCopyProvider
{
    public string WelcomeHeadline(string firstName)
        => $"Welcome back, {firstName} — here's where you are today.";

    public string WelcomeSubhead()
        => "We've kept track of everything since your last visit.";

    public string AwaitingActionDraft(string projectName)
        => $"Your draft for {projectName} is ready to send.";

    public string AwaitingActionSentBack(string projectName)
        => $"We need a few more details on {projectName} before we can decide.";

    public string AwaitingActionAgreement(string projectName)
        => $"Your funding agreement for {projectName} is ready to sign.";

    public string EmptyHeroHeadline() => "Ready to apply for funding?";
    public string EmptyHeroSubhead() => "Tell us about your project — we'll guide you the rest of the way.";
    public string EmptyCtaLabel() => "Start a new application";

    public string ResourcesHowFundingWorks() => "How funding works";
    public string ResourcesSubmissionTips()  => "Submission tips";
    public string ResourcesGetHelp()         => "Get help";

    public string TrustHowLongTitle() => "How long it takes";
    public string TrustHowLongBody()  => "Most applications get a decision within 3 weeks of being sent.";
    public string TrustWhatYouNeedTitle() => "What you'll need";
    public string TrustWhatYouNeedBody()  => "A short project description, item list, and one quotation per item.";
    public string TrustHowDecisionsTitle() => "How decisions are made";
    public string TrustHowDecisionsBody()  => "Reviewers check completeness, fit, and quotations — we'll tell you the reasoning either way.";
}

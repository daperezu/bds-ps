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
        SigningCeremonyVariant.ApplicantOnlySigned       => "Su firma quedó registrada.",
        SigningCeremonyVariant.FunderOnlySigned          => "Firma del aportante registrada.",
        SigningCeremonyVariant.BothCompleteApplicantLast => "Su financiamiento está confirmado.",
        SigningCeremonyVariant.BothCompleteFunderLast    => "Su financiamiento está confirmado.",
        _                                                => "Su firma quedó registrada.",
    };

    public string Subhead(SigningCeremonyVariant variant, DateOnly date) => variant switch
    {
        SigningCeremonyVariant.ApplicantOnlySigned => "Estamos a la espera del aportante. Le avisaremos por correo cuando esté completo.",
        SigningCeremonyVariant.FunderOnlySigned    => "Se ha notificado al solicitante.",
        SigningCeremonyVariant.BothCompleteApplicantLast => $"Los fondos se transferirán antes del {date:dd/MM/yyyy}.",
        SigningCeremonyVariant.BothCompleteFunderLast    => $"Los fondos se transferirán antes del {date:dd/MM/yyyy}.",
        _ => string.Empty,
    };

    public bool ShouldFireConfetti(SigningCeremonyVariant variant)
        => variant is SigningCeremonyVariant.BothCompleteApplicantLast
                   or SigningCeremonyVariant.BothCompleteFunderLast;

    public string AriaLiveAnnouncement(SigningCeremonyVariant variant) => variant switch
    {
        SigningCeremonyVariant.BothCompleteApplicantLast => "Su convenio de financiamiento está firmado.",
        SigningCeremonyVariant.BothCompleteFunderLast    => "Su convenio de financiamiento está firmado.",
        SigningCeremonyVariant.ApplicantOnlySigned       => "Su firma fue registrada.",
        SigningCeremonyVariant.FunderOnlySigned          => "La firma del aportante fue registrada.",
        _                                                => "Evento de firma registrado.",
    };

    public string PrimaryCtaLabel(SigningCeremonyVariant variant) => "Ver detalles del financiamiento";
    public string SecondaryCtaLabel() => "Volver al panel";
}

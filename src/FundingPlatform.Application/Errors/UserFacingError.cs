namespace FundingPlatform.Application.Errors;

/// <summary>
/// Spec 012 / FR-014 — paired error code + optional English detail returned by
/// Application services across the Web boundary.
///
/// <para>
/// <see cref="Code"/> is the stable identifier the Web layer translates to
/// es-CR (via <c>IUserFacingErrorTranslator</c>). <see cref="Detail"/> is an
/// optional English string with extra context (e.g. the original
/// <c>InvalidOperationException.Message</c> from a domain rule). Detail is
/// suitable for logs and developer surfaces; it MUST NOT be displayed to the
/// user verbatim — the translator may incorporate sanitized parts of it but
/// the user-visible string is always derived from <see cref="Code"/>.
/// </para>
/// </summary>
public sealed record UserFacingError(UserFacingErrorCode Code, string? Detail = null)
{
    public static UserFacingError From(UserFacingErrorCode code) => new(code);
    public static UserFacingError From(UserFacingErrorCode code, string detail) => new(code, detail);
}

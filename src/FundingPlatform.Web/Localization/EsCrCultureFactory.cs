using System.Globalization;

namespace FundingPlatform.Web.Localization;

/// <summary>
/// Builds the request culture for the platform: Costa Rican Spanish ("es-CR")
/// with format overrides that match the spec's user-visible target.
///
/// .NET's es-CR defaults render numbers as "1 234 567,89" and dates as
/// "29/4/2026"; CR business documents use US-style separators ("1,234,567.89")
/// and two-digit dates ("29/04/2026"). We clone the culture and override the
/// formatter properties at startup.
///
/// See spec 012 research Decision 1.
/// </summary>
public static class EsCrCultureFactory
{
    /// <summary>
    /// Build the cloned + format-overridden es-CR <see cref="CultureInfo"/>.
    /// The returned instance is read-only to prevent accidental mutation
    /// downstream (request handlers must not reach into NumberFormat / DateTimeFormat
    /// and twiddle separators).
    /// </summary>
    public static CultureInfo Build()
    {
        var ci = (CultureInfo)CultureInfo.GetCultureInfo("es-CR").Clone();

        // Number format: period decimal, comma thousands.
        ci.NumberFormat.NumberDecimalSeparator = ".";
        ci.NumberFormat.NumberGroupSeparator = ",";
        ci.NumberFormat.CurrencyDecimalSeparator = ".";
        ci.NumberFormat.CurrencyGroupSeparator = ",";
        ci.NumberFormat.PercentDecimalSeparator = ".";
        ci.NumberFormat.PercentGroupSeparator = ",";

        // Date format: two-digit day/month.
        ci.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";

        return CultureInfo.ReadOnly(ci);
    }
}

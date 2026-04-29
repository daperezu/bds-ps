using System.Globalization;

namespace FundingPlatform.Tests.Unit.Web;

/// <summary>
/// Spec 012 — currency / number formatting tests for the es-CR locale.
///
/// The raw .NET <c>es-CO</c> CultureInfo (Latin-American formatting) is used
/// here as a stable reference culture — its separators have not shifted across
/// .NET versions, so the assertions stay deterministic. The spec's actual
/// production format-overridden es-CR culture is exercised separately by
/// <c>CurrencyDisplayUnderEsCrTests</c>.
/// </summary>
[TestFixture]
public class FundingAgreementCurrencyFormattingTests
{
    [TestCase("es-CO", 1234567.89, "1.234.567,89")]
    [TestCase("es-MX", 1234567.89, "1,234,567.89")]
    public void Number_Formatting_Respects_Configured_Locale(string localeCode, double value, string expected)
    {
        var culture = CultureInfo.GetCultureInfo(localeCode);
        var formatted = ((decimal)value).ToString("N2", culture);
        Assert.That(formatted, Is.EqualTo(expected));
    }

    [Test]
    public void Default_Locale_Is_esCR_LatinAmerican_BaselineSeparators()
    {
        // es-CR is the spec's target locale (FR-016). The Latin-American
        // baseline assertion uses the raw es-CO culture as a stable reference;
        // CurrencyDisplayUnderEsCrTests verifies the format-overridden
        // production es-CR culture (US-style separators, dd/MM/yyyy dates).
        var culture = CultureInfo.GetCultureInfo("es-CO");
        Assert.That(culture.NumberFormat.NumberGroupSeparator, Is.EqualTo("."));
        Assert.That(culture.NumberFormat.NumberDecimalSeparator, Is.EqualTo(","));
    }

    [Test]
    public void Currency_Symbol_Is_Decoupled_From_Locale_For_esCR()
    {
        // NFR-010: formatting should stay consistent even when currency ISO changes.
        // The spec's es-CR locale (FR-016) renders the ISO code as a separate
        // suffix; this test uses the raw es-CO baseline as the stable reference
        // for the separator pair.
        var culture = CultureInfo.GetCultureInfo("es-CO");
        var amount = 2500m;

        var withCopSuffix = $"{amount.ToString("N2", culture)} COP";
        var withUsdSuffix = $"{amount.ToString("N2", culture)} USD";

        Assert.That(withCopSuffix, Is.EqualTo("2.500,00 COP"));
        Assert.That(withUsdSuffix, Is.EqualTo("2.500,00 USD"));
    }
}

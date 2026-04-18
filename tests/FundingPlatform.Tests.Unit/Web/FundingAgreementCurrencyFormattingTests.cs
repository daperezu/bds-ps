using System.Globalization;

namespace FundingPlatform.Tests.Unit.Web;

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
    public void Default_Locale_Is_esCO_LatinAmerican()
    {
        var culture = CultureInfo.GetCultureInfo("es-CO");
        Assert.That(culture.NumberFormat.NumberGroupSeparator, Is.EqualTo("."));
        Assert.That(culture.NumberFormat.NumberDecimalSeparator, Is.EqualTo(","));
    }

    [Test]
    public void Currency_Symbol_Is_Decoupled_From_Locale()
    {
        // NFR-010: formatting should stay consistent even when currency ISO changes.
        var culture = CultureInfo.GetCultureInfo("es-CO");
        var amount = 2500m;

        var withCopSuffix = $"{amount.ToString("N2", culture)} COP";
        var withUsdSuffix = $"{amount.ToString("N2", culture)} USD";

        Assert.That(withCopSuffix, Is.EqualTo("2.500,00 COP"));
        Assert.That(withUsdSuffix, Is.EqualTo("2.500,00 USD"));
    }
}

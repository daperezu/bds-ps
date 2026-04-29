using System.Globalization;
using FundingPlatform.Web.Localization;

namespace FundingPlatform.Tests.Integration.Web;

/// <summary>
/// Spec 012 / FR-019 / T028a — verifies that the format-overridden es-CR culture
/// (the one that backs both the request culture and the PDF partials) renders
/// quotation amounts with the spec's user-visible target separators
/// ("1,234.56" — period decimal, comma thousands) regardless of the per-quotation
/// Currency code (CRC, USD, GBP) introduced in spec 010.
///
/// Spec 010's per-quotation Currency code controls the ISO code rendering;
/// es-CR formatting only governs the separators and the date format.
/// </summary>
[TestFixture]
public class CurrencyDisplayUnderEsCrTests
{
    private CultureInfo _esCr = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _esCr = EsCrCultureFactory.Build();
    }

    [TestCase("CRC", 1234.56)]
    [TestCase("USD", 1234.56)]
    [TestCase("GBP", 1234.56)]
    public void Quotation_amount_renders_es_cr_separators_for_every_currency(string isoCode, double rawAmount)
    {
        // Mirrors the rendering pattern used in
        // Views/FundingAgreement/Partials/_FundingAgreementItemsTable.cshtml and
        // Views/Admin/Reports/* — culture-driven N2 formatting plus a per-quotation
        // ISO code prefix (or suffix). The shared assertion is the separator pair.
        var amount = (decimal)rawAmount;
        var formatted = $"{isoCode} {amount.ToString("N2", _esCr)}";

        Assert.That(formatted, Does.Contain($"{isoCode} 1,234.56"),
            $"FR-019: Expected es-CR-formatted '1,234.56' alongside ISO code '{isoCode}', got '{formatted}'.");
    }

    [TestCase(1234567.89, "1,234,567.89")]
    [TestCase(0.5, "0.50")]
    [TestCase(1000, "1,000.00")]
    public void Format_overrides_apply_for_a_range_of_amounts(double rawAmount, string expectedFormatted)
    {
        var amount = (decimal)rawAmount;
        var formatted = amount.ToString("N2", _esCr);
        Assert.That(formatted, Is.EqualTo(expectedFormatted),
            $"FR-017 / SC-005: format-overridden es-CR must render {rawAmount} as '{expectedFormatted}', got '{formatted}'.");
    }

    [Test]
    public void Date_format_is_dd_MM_yyyy()
    {
        var date = new DateTime(2026, 4, 29);
        var formatted = date.ToString("d", _esCr);
        Assert.That(formatted, Is.EqualTo("29/04/2026"),
            "FR-018 / SC-005: format-overridden es-CR must render dates as dd/MM/yyyy.");
    }
}

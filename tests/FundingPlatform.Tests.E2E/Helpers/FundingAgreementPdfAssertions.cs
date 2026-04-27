using System.Text.RegularExpressions;
using Syncfusion.Pdf.Parsing;

namespace FundingPlatform.Tests.E2E.Helpers;

public static class FundingAgreementPdfAssertions
{
    public static string ExtractText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var doc = new PdfLoadedDocument(stream);
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < doc.Pages.Count; i++)
        {
            sb.Append(doc.Pages[i].ExtractText());
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public static void AssertEachAmountHasCurrencyCode(byte[] pdfBytes, IReadOnlyCollection<string> expectedCurrencies)
    {
        var text = ExtractText(pdfBytes);
        foreach (var currency in expectedCurrencies)
        {
            Assert.That(text, Does.Contain(currency),
                $"Expected currency code '{currency}' to appear at least once in the PDF text.");
        }
    }

    public static void AssertNoBareDecimalAmounts(byte[] pdfBytes)
    {
        // Items table renders amounts as "{ISO} {N2-formatted}". A bare amount in the
        // items table would lack the 3-letter currency prefix on the same line.
        var text = ExtractText(pdfBytes);
        var lines = text.Split('\n');
        var bareAmount = new Regex(@"^\s*[\d.,]+\s*$", RegexOptions.Compiled);
        foreach (var line in lines)
        {
            Assert.That(bareAmount.IsMatch(line), Is.False,
                $"PDF contains a line that looks like a bare decimal amount with no currency prefix: '{line.Trim()}'.");
        }
    }
}

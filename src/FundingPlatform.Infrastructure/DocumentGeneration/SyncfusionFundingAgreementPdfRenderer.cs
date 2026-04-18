using FundingPlatform.Application.Interfaces;
using Syncfusion.HtmlConverter;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace FundingPlatform.Infrastructure.DocumentGeneration;

public class SyncfusionFundingAgreementPdfRenderer : IFundingAgreementPdfRenderer
{
    public Task<byte[]> RenderAsync(string html, string? baseUrl)
    {
        var converter = new HtmlToPdfConverter(HtmlRenderingEngine.Blink);

        var blinkSettings = new BlinkConverterSettings
        {
            PdfPageSize = PdfPageSize.A4,
            Margin = new PdfMargins { All = 36 },
            EnableJavaScript = false
        };

        converter.ConverterSettings = blinkSettings;

        using var document = converter.Convert(html, baseUrl ?? string.Empty);
        using var stream = new MemoryStream();
        document.Save(stream);
        document.Close(true);

        return Task.FromResult(stream.ToArray());
    }
}

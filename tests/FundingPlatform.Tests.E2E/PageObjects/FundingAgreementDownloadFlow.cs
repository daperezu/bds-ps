using Microsoft.Playwright;

namespace FundingPlatform.Tests.E2E.PageObjects;

public class FundingAgreementDownloadFlow : BasePage
{
    public FundingAgreementDownloadFlow(IPage page) : base(page)
    {
    }

    public async Task<byte[]> CaptureDownloadBytesAsync(ILocator downloadTrigger)
    {
        var downloadTask = Page.WaitForDownloadAsync();
        await downloadTrigger.ClickAsync();
        var download = await downloadTask;

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"funding-agreement-{Guid.NewGuid():N}.pdf");

        await download.SaveAsAsync(tempPath);
        try
        {
            return await File.ReadAllBytesAsync(tempPath);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort */ }
        }
    }

    public static bool LooksLikePdf(byte[] bytes)
    {
        if (bytes.Length < 5) return false;
        return bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F';
    }
}

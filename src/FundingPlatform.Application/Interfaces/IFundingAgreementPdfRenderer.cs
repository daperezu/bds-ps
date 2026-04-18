namespace FundingPlatform.Application.Interfaces;

public interface IFundingAgreementPdfRenderer
{
    Task<byte[]> RenderAsync(string html, string? baseUrl);
}

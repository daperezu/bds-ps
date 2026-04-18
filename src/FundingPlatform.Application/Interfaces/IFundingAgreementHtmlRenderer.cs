namespace FundingPlatform.Application.Interfaces;

public interface IFundingAgreementHtmlRenderer
{
    Task<string> RenderAsync<TModel>(string viewPath, TModel model);
}

namespace FundingPlatform.Application.Applications.Commands;

public class ReplaceQuotationDocumentCommand
{
    public int ApplicationId { get; set; }
    public int ItemId { get; set; }
    public int QuotationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

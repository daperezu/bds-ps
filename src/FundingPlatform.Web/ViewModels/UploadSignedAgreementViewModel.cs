using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FundingPlatform.Web.ViewModels;

public class UploadSignedAgreementViewModel
{
    [Required]
    public int GeneratedVersion { get; set; }

    [Required]
    public IFormFile File { get; set; } = default!;
}

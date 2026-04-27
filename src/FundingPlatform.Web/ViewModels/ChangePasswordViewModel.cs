using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string OldPassword { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = "";
}

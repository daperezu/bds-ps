using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUserResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = "";

    public string TargetEmail { get; set; } = "";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
    public string NewTemporaryPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(NewTemporaryPassword))]
    public string ConfirmPassword { get; set; } = "";
}

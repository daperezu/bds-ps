using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUserCreateViewModel
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = "";

    [Phone]
    public string? Phone { get; set; }

    [Required]
    public string Role { get; set; } = "Applicant";

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
    public string InitialPassword { get; set; } = "";

    [StringLength(50)]
    public string? LegalId { get; set; }
}

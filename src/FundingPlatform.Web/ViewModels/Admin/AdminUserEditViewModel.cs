using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUserEditViewModel
{
    [Required]
    public string UserId { get; set; } = "";

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

    [StringLength(50)]
    public string? LegalId { get; set; }
}

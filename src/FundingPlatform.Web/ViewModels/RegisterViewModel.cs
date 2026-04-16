using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm Password"), Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, Display(Name = "First Name"), MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Last Name"), MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, Display(Name = "Legal ID"), MaxLength(50)]
    public string LegalId { get; set; } = string.Empty;
}

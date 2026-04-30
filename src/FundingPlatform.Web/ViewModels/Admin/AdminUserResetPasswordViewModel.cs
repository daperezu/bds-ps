using System.ComponentModel.DataAnnotations;

namespace FundingPlatform.Web.ViewModels.Admin;

public class AdminUserResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = "";

    public string TargetEmail { get; set; } = "";

    [Required(ErrorMessage = "La nueva contraseña temporal es obligatoria.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres.")]
    [Display(Name = "Nueva contraseña temporal")]
    public string NewTemporaryPassword { get; set; } = "";

    [Required(ErrorMessage = "Debe confirmar la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    [Compare(nameof(NewTemporaryPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = "";
}

namespace FundingPlatform.Web.Controllers.Admin;

internal static class AdminErrorMessages
{
    public const string SentinelImmutable =
        "Esta cuenta es del sistema y no se puede modificar.";

    public const string LastAdminProtected =
        "No se puede inhabilitar al último administrador. Promueva a otro usuario al rol Administrador primero.";

    public const string SelfDisable =
        "Los administradores no pueden inhabilitar su propia cuenta.";

    public const string SelfChangeRole =
        "Los administradores no pueden cambiar su propio rol desde el área de administración.";

    public const string SelfChangeEmail =
        "Los administradores no pueden cambiar su propio correo electrónico desde el área de administración.";

    public const string SelfResetPassword =
        "Los administradores no pueden restablecer su propia contraseña desde el área de administración.";
}

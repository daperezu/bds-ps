using Microsoft.AspNetCore.Identity;

namespace FundingPlatform.Infrastructure.Identity;

/// <summary>
/// Costa Rican Spanish (es-CR) Identity error descriptions.
///
/// Overrides every virtual method on <see cref="IdentityErrorDescriber"/> so
/// that any Identity error code surfaced by the framework — including codes
/// the platform does not currently exercise — produces a Spanish description
/// by default.
///
/// Wording follows the voice guide (formal usted, warm-modern tone).
/// See spec 012 data-model.md §3.
/// </summary>
public sealed class EsCrIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = "Ocurrió un error inesperado.",
    };

    public override IdentityError ConcurrencyFailure() => new()
    {
        Code = nameof(ConcurrencyFailure),
        Description = "El registro fue modificado por otro usuario.",
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = "Contraseña incorrecta.",
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = "El token no es válido.",
    };

    public override IdentityError LoginAlreadyAssociated() => new()
    {
        Code = nameof(LoginAlreadyAssociated),
        Description = "Ya existe un usuario asociado a este inicio de sesión.",
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = $"El nombre de usuario '{userName}' no es válido. Solo se permiten letras y dígitos.",
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = $"El correo electrónico '{email}' no es válido.",
    };

    public override IdentityError DuplicateUserName(string userName) => new()
    {
        Code = nameof(DuplicateUserName),
        Description = $"El nombre de usuario '{userName}' ya está en uso.",
    };

    public override IdentityError DuplicateEmail(string email) => new()
    {
        Code = nameof(DuplicateEmail),
        Description = $"El correo electrónico '{email}' ya está registrado.",
    };

    public override IdentityError InvalidRoleName(string? role) => new()
    {
        Code = nameof(InvalidRoleName),
        Description = $"El nombre del rol '{role}' no es válido.",
    };

    public override IdentityError DuplicateRoleName(string role) => new()
    {
        Code = nameof(DuplicateRoleName),
        Description = $"El nombre del rol '{role}' ya está en uso.",
    };

    public override IdentityError UserAlreadyHasPassword() => new()
    {
        Code = nameof(UserAlreadyHasPassword),
        Description = "El usuario ya tiene una contraseña establecida.",
    };

    public override IdentityError UserLockoutNotEnabled() => new()
    {
        Code = nameof(UserLockoutNotEnabled),
        Description = "El bloqueo de cuenta no está habilitado para este usuario.",
    };

    public override IdentityError UserAlreadyInRole(string role) => new()
    {
        Code = nameof(UserAlreadyInRole),
        Description = $"El usuario ya tiene el rol '{role}'.",
    };

    public override IdentityError UserNotInRole(string role) => new()
    {
        Code = nameof(UserNotInRole),
        Description = $"El usuario no tiene el rol '{role}'.",
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = $"La contraseña debe tener al menos {length} caracteres.",
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = "La contraseña debe incluir al menos un carácter no alfanumérico.",
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = "La contraseña debe incluir al menos un dígito ('0'-'9').",
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = "La contraseña debe incluir al menos una letra minúscula ('a'-'z').",
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = "La contraseña debe incluir al menos una letra mayúscula ('A'-'Z').",
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = $"La contraseña debe contener al menos {uniqueChars} caracteres distintos.",
    };

    public override IdentityError RecoveryCodeRedemptionFailed() => new()
    {
        Code = nameof(RecoveryCodeRedemptionFailed),
        Description = "No se pudo canjear el código de recuperación.",
    };
}

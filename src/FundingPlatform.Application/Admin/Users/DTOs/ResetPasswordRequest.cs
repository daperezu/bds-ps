namespace FundingPlatform.Application.Admin.Users.DTOs;

public record ResetPasswordRequest(
    string UserId,
    string NewTemporaryPassword);

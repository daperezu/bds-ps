using FundingPlatform.Application.Admin.Users.DTOs;

namespace FundingPlatform.Application.Admin.Users;

public interface IUserAdministrationService
{
    Task<ListUsersResult> ListUsersAsync(ListUsersRequest request, CancellationToken ct);
    Task<UserDetailDto?> GetUserAsync(string userId, CancellationToken ct);
    Task<Result<UserDetailDto>> CreateUserAsync(CreateUserRequest request, string actorUserId, CancellationToken ct);
    Task<Result<UserDetailDto>> UpdateUserAsync(UpdateUserRequest request, string actorUserId, CancellationToken ct);
    Task<Result> DisableUserAsync(string targetUserId, string actorUserId, CancellationToken ct);
    Task<Result> EnableUserAsync(string targetUserId, string actorUserId, CancellationToken ct);
    Task<Result> ResetUserPasswordAsync(ResetPasswordRequest request, string actorUserId, CancellationToken ct);
}

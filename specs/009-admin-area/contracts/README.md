# Contracts: Admin Role and Admin Area

This file documents the controller routes/actions, the `IUserAdministrationService` interface (Application layer), the request/response DTO shapes, and the named error-code map. It is the contract surface between the Web layer and the Application layer (and between the controllers and the Razor views via the view-models).

---

## 1. HTTP routes (Web layer)

### `AdminUsersController` — `[Route("Admin/Users")]`, `[Authorize(Roles = "Admin")]`

| Verb | Path | Action | Purpose |
|------|------|--------|---------|
| GET  | `/Admin/Users` | `Index(string? roleFilter, string? statusFilter, string? search, int page = 1, int pageSize = 20)` | Render the paginated, filtered user listing. |
| GET  | `/Admin/Users/Create` | `Create()` | Render the create-user form (empty `AdminUserCreateViewModel`). |
| POST | `/Admin/Users/Create` | `Create(AdminUserCreateViewModel vm)` | Create the user; on success redirect to `/Admin/Users` with `TempData["SuccessMessage"]`; on validation failure re-render the form with errors collected together. |
| GET  | `/Admin/Users/{id}/Edit` | `Edit(string id)` | Render the edit form (loaded via `IUserAdministrationService.GetUserAsync`). 404 if id not found OR the target is the sentinel (the global filter hides it from `Users.Where(u => u.Id == id)`; the controller does not reach the guard). |
| POST | `/Admin/Users/{id}/Edit` | `Edit(string id, AdminUserEditViewModel vm)` | Apply edits (email, name, phone, role, legal-id-when-Applicant). On success redirect to `/Admin/Users` with success message; on guard-rejection (`SentinelUserModificationException`, `LastAdministratorException`, `SelfModificationException`) re-render the form with the named-rule error in `ModelState`. |
| POST | `/Admin/Users/{id}/Disable` | `Disable(string id)` | After confirmation dialog (client-side via `_ConfirmDialog`). Calls service; redirects to `/Admin/Users` with success or error message. |
| POST | `/Admin/Users/{id}/Enable` | `Enable(string id)` | No confirmation needed (re-enabling is non-destructive). Redirects with success. |
| GET  | `/Admin/Users/{id}/ResetPassword` | `ResetPassword(string id)` | Render the reset-password form (`AdminUserResetPasswordViewModel`). |
| POST | `/Admin/Users/{id}/ResetPassword` | `ResetPassword(string id, AdminUserResetPasswordViewModel vm)` | Apply new temp password; redirect to `/Admin/Users` with success. |

### `AdminReportsController` — `[Route("Admin/Reports")]`, `[Authorize(Roles = "Admin")]`

| Verb | Path | Action | Purpose |
|------|------|--------|---------|
| GET  | `/Admin/Reports` | `Index()` | Render the stub page (empty-state). |

### `AccountController` — `/Account/...` (existing, modified)

| Verb | Path | Action | Purpose |
|------|------|--------|---------|
| GET  | `/Account/AccessDenied` | `AccessDenied()` | **NEW**. Returns 403 with `Views/Account/AccessDenied.cshtml`. |
| GET  | `/Account/ChangePassword` | `ChangePassword()` | **NEW**. Renders the forced-change form. Available to any authenticated user. |
| POST | `/Account/ChangePassword` | `ChangePassword(ChangePasswordViewModel vm)` | **NEW**. Validates old + new password, calls `UserManager.ChangePasswordAsync`, clears `MustChangePassword`, rotates security stamp, redirects to home. |
| POST | `/Account/Login` | (existing, modified) | After successful sign-in, if `user.MustChangePassword` is true, redirect to `/Account/ChangePassword` instead of the usual landing page. |

---

## 2. `IUserAdministrationService` (Application layer)

```text
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
```

Where `Result` and `Result<T>` are the existing project's success/failure-with-error-list types (re-used from prior specs; if absent, introduced minimally as `record Result(bool Succeeded, IReadOnlyList<DomainError> Errors)` and `Result<T> : Result` adding `T? Value`). `DomainError` carries `(string Code, string? Field, string Message)`.

**Method semantics** (each method internally applies the three guards in order — sentinel, self-modification, last-admin — before any mutation):

- **`ListUsersAsync`**: returns a paginated `ListUsersResult`. Sentinel is never in the result (global filter applied automatically).
- **`GetUserAsync`**: returns null if the id is missing OR the user is the sentinel. The controller's 404 path covers both.
- **`CreateUserAsync`**: validates input, creates `ApplicationUser`, sets `MustChangePassword = true`, calls `UserManager.CreateAsync`, then `AddToRoleAsync(role)`. If `role == Applicant`, also persists `Applicant` row (reusing existing row if `UserId` already has one — supports re-promotion of a previously-Applicant user). All in one transaction; rolls back on any failure.
- **`UpdateUserAsync`**: looks up target via `_dbContext.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == request.UserId)`. If sentinel, throws `SentinelUserModificationException`. If actor.Id == target.Id and the request changes role/email/disable-state, throws `SelfModificationException`. If the role-change would leave zero active non-sentinel admins, throws `LastAdministratorException`. Otherwise applies email change (with security-stamp rotation), name/phone/legal-id changes, and role change (with security-stamp rotation).
- **`DisableUserAsync`**: same guard ordering. Sets `LockoutEnd = DateTimeOffset.MaxValue`, rotates security stamp.
- **`EnableUserAsync`**: same guard ordering except last-admin guard does not apply (re-enable always increases the active-admin count). Sets `LockoutEnd = null`.
- **`ResetUserPasswordAsync`**: same guard ordering. Calls `UserManager.RemovePasswordAsync` then `UserManager.AddPasswordAsync(newPassword)` (or equivalent), sets `MustChangePassword = true`, rotates security stamp.

---

## 3. DTOs (Application layer, plain records)

```text
public record ListUsersRequest(
    string? RoleFilter,         // "Applicant" | "Reviewer" | "Admin" | null = all
    string? StatusFilter,       // "Active" | "Disabled" | null = all
    string? Search,             // free-text matched against email, FirstName, LastName
    int Page,
    int PageSize);

public record ListUsersResult(
    IReadOnlyList<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record UserSummaryDto(
    string Id,
    string FullName,
    string Email,
    string Role,
    string Status,              // "Active" | "Disabled"
    DateTimeOffset CreatedAt);  // taken from Identity-managed creation tracking; or from a creation-stamped lookup if Identity does not store creation date — see implementation note in research.md (defer concrete source to implementation).

public record UserDetailDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string Status,
    string? LegalId,            // populated only when Role == "Applicant"
    bool MustChangePassword);

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string InitialPassword,
    string? LegalId);           // required when Role == "Applicant", null otherwise

public record UpdateUserRequest(
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    string? LegalId);

public record ResetPasswordRequest(
    string UserId,
    string NewTemporaryPassword);

public record DomainError(string Code, string? Field, string Message);
```

---

## 4. View-models (Web layer, with DataAnnotations)

```text
public class AdminUsersListViewModel
{
    public IReadOnlyList<AdminUserSummaryRowViewModel> Rows { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? RoleFilter { get; init; }
    public string? StatusFilter { get; init; }
    public string? Search { get; init; }
}

public class AdminUserSummaryRowViewModel
{
    public string Id { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsSelf { get; init; }            // actor == row → disables the Disable / change-role / change-email controls in the row's action menu
}

public class AdminUserCreateViewModel
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
    [Phone] public string? Phone { get; set; }
    [Required] public string Role { get; set; } = "Applicant";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)] public string InitialPassword { get; set; } = "";
    [StringLength(50)] public string? LegalId { get; set; }   // server-side enforced as Required when Role == "Applicant"
}

public class AdminUserEditViewModel : AdminUserCreateViewModel
{
    [Required] public string UserId { get; set; } = "";
    public new string? InitialPassword => null;   // not on edit form
}

public class AdminUserResetPasswordViewModel
{
    [Required] public string UserId { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)] public string NewTemporaryPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewTemporaryPassword))] public string ConfirmPassword { get; set; } = "";
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)] public string OldPassword { get; set; } = "";
    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)] public string NewPassword { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}
```

---

## 5. Sidebar entry table

| Display | URL | Role gate | Source partial |
|---------|-----|-----------|----------------|
| Users | `/Admin/Users` | `Admin` | extension of spec 008's role-aware sidebar partial |
| Reports | `/Admin/Reports` | `Admin` | same |

Existing sidebar entries from prior specs are unchanged. Reviewer-only entries continue to render for users in the Reviewer role; Admins also see them because of the `AdminImpliesReviewerClaimsTransformation` (research Decision 3).

---

## 6. Error-code map

| Code | HTTP shape (controller mapping) | UI surface | Trigger |
|------|---------------------------------|-----------|---------|
| `SENTINEL_IMMUTABLE` | 400 + re-render with named-rule error | "This account is a system account and cannot be modified." | Any write targeting the sentinel. |
| `LAST_ADMIN_PROTECTED` | 400 + re-render with named-rule error | "Cannot disable the last remaining administrator. Promote another user to Admin first." | Disable / role-change / etc. that would leave zero active non-sentinel admins. |
| `SELF_MODIFICATION_BLOCKED` | 400 + re-render with named-rule error | "Administrators cannot {disable themselves \| change their own role \| change their own email} from the admin area." (depending on `Action` enum) | Self-disable, self-demote, self-email-change from admin area. |
| `EMAIL_IN_USE` | 400 + re-render with field error on `Email` | "Email already in use by another account." | Email collision on create or edit. Includes the case where someone tries to create a user with the sentinel email. |
| `LEGAL_ID_IN_USE` | 400 + re-render with field error on `LegalId` | "Legal ID already in use by another applicant." | Legal-id collision on Applicant create or applicant-role-promotion. |
| `WEAK_PASSWORD` | 400 + re-render with each Identity password error as a separate inline error | (Identity's per-rule messages) | Identity's password policy rejects the new password. |
| `INVALID_INPUT` | 400 + re-render with field-level errors from `ModelState` | (per field) | Standard validation failures (missing required, bad email format, etc.). |

---

## 7. Configuration keys (introduced or modified)

| Key | Type | Purpose | Default |
|-----|------|---------|---------|
| `Admin:DefaultPassword` | string | Override the auto-generated sentinel password on first deploy. | unset → auto-generate + WARN-log |
| `IdentityOptions.SecurityStamp.ValidationInterval` | TimeSpan | Cookie revalidation cadence. | `1 minute` (changed from default `30 minutes`). |
| `CookieAuthenticationOptions.AccessDeniedPath` | string | Path for authenticated-but-unauthorized requests. | `/Account/AccessDenied` (changed from `/Account/Login`). |
| `CookieAuthenticationOptions.LoginPath` | string | Path for unauthenticated requests. | `/Account/Login` (unchanged). |

---

## 8. Out-of-scope contracts

- **No JSON API**. Everything is server-rendered MVC; no `Controllers/Api/` is added.
- **No webhooks, no events, no message-bus contracts**. Audit-log eventing is deferred to a future spec.
- **No CLI**. The sentinel is seeded at application startup, not via a dotnet-run-CLI command.
- **No GraphQL**. Out of stack.

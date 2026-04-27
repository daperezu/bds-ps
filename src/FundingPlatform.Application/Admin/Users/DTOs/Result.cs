namespace FundingPlatform.Application.Admin.Users.DTOs;

public record Result(bool Succeeded, IReadOnlyList<DomainError> Errors)
{
    public static Result Success() => new(true, Array.Empty<DomainError>());
    public static Result Failure(params DomainError[] errors) => new(false, errors);
    public static Result Failure(IReadOnlyList<DomainError> errors) => new(false, errors);
}

public record Result<T>(bool Succeeded, T? Value, IReadOnlyList<DomainError> Errors)
    : Result(Succeeded, Errors)
{
    public static Result<T> Success(T value) => new(true, value, Array.Empty<DomainError>());
    public static new Result<T> Failure(params DomainError[] errors) => new(false, default, errors);
    public static new Result<T> Failure(IReadOnlyList<DomainError> errors) => new(false, default, errors);
}

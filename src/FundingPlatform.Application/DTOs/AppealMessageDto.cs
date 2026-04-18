namespace FundingPlatform.Application.DTOs;

public record AppealMessageDto(
    int Id,
    string AuthorUserId,
    string AuthorDisplayName,
    bool IsByApplicant,
    string Text,
    DateTime CreatedAt);

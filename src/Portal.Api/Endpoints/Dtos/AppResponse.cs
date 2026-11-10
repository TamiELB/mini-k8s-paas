namespace Portal.Api.Endpoints.Dtos;

public sealed record AppResponse(
    Guid Id,
    string CustomerId,
    string Name,
    string Status,
    string? Url,
    string? InternalAddress,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
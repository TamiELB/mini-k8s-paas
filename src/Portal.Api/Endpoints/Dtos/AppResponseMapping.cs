using Portal.Api.Domain;

namespace Portal.Api.Endpoints.Dtos;

public static class AppResponseMapping
{
    public static AppResponse ToResponse(this App app) => new(
        app.Id,
        app.CustomerId,
        app.Name,
        app.Status.ToString(),
        app.Url,
        app.InternalAdress,
        app.FailureReason,
        app.CreatedAt,
        app.UpdatedAt);
}
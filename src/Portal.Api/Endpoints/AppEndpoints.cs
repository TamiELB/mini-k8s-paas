using MassTransit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Portal.Api.Domain;
using Portal.Api.Endpoints.Dtos;
using Portal.Api.Persistence;

namespace Portal.Api.Endpoints;

public static class AppEndpoints
{
    public static void MapAppEndpoints(this IEndpointRouteBuilder routes)
    {
        var apps = routes.MapGroup("/apps");

        apps.MapPost("/", CreateAsync);
        apps.MapGet("/{id:guid}", GetAsync);
    }

    private static async Task<IResult> CreateAsync(
        CreateAppRequest request,
        PortalDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        var nameTaken = await dbContext.Apps.AnyAsync(
            a => a.CustomerId == request.CustomerId && a.Name == request.AppName,
            cancellationToken);

        if (nameTaken)
            return Results.Conflict(new { error = $"App '{request.AppName}' already exists for customer '{request.CustomerId}'." });

        var appId = Guid.CreateVersion7();
        var app = App.Request(appId, request.CustomerId, request.AppName, DateTimeOffset.UtcNow);
        var message = request.ToAppRequested(appId);

        dbContext.Apps.Add(app);
        await publishEndpoint.Publish(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Accepted($"/apps/{app.Id}", new { id = app.Id, status = app.Status.ToString() });
    }

    private static async Task<Results<Ok<AppResponse>, NotFound>> GetAsync(
        Guid id,
        PortalDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var app = await dbContext.Apps.FindAsync([id], cancellationToken);
        if (app is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(app.ToResponse());
    }
}
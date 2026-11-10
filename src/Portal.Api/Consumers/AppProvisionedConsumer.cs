using Contracts;
using MassTransit;
using Portal.Api.Domain;
using Portal.Api.Persistence;

namespace Portal.Api.Consumers;

public class AppProvisionedConsumer(PortalDbContext dbContext, ILogger<AppProvisionedConsumer> logger)
    : IConsumer<AppProvisioned>
{
    public async Task Consume(ConsumeContext<AppProvisioned> context)
    {
        var msg = context.Message;

        var app = await dbContext.Apps.FindAsync([msg.AppId], context.CancellationToken);
        if (app is null)
        {
            logger.LogWarning("App {AppId} not found", msg.AppId);
            return;
        }

        if (app.Status != AppStatus.Requested)
        {
            logger.LogInformation("App {AppId} already {Status}, ignoring", msg.AppId, app.Status);
            return;
        }

        app.MarkRunning(msg.Url, msg.InternalAddress, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
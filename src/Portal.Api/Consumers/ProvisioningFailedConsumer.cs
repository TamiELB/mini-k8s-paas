using Contracts;
using MassTransit;
using Portal.Api.Domain;
using Portal.Api.Persistence;

namespace Portal.Api.Consumers;

public class ProvisioningFailedConsumer(PortalDbContext dbContext, ILogger<ProvisioningFailedConsumer> logger)
    :  IConsumer<ProvisioningFailed>
{
    public async Task Consume(ConsumeContext<ProvisioningFailed> context)
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

        app.MarkFailed(msg.Reason, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
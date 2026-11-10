using Contracts;
using MassTransit;
using Provisioner.Service.Provisioning;

namespace Provisioner.Service.Consumers;

public class AppRequestedConsumer(IKubernetesProvisioner provisioner, ILogger<AppRequestedConsumer> logger)
    : IConsumer<AppRequested>
{
    public async Task Consume(ConsumeContext<AppRequested> context)
    {
        var msg = context.Message;

        AppSpec spec;
        try
        {
            spec = new AppSpec(
                msg.CustomerId,
                msg.AppName,
                new WorkloadSpec(msg.Workload.Image, msg.Workload.Replicas, msg.Workload.Env, msg.Workload.Secrets),
                msg.Service is { } s ? new ServiceSpec(s.Port, s.TargetPort) : null,
                msg.Ingress is { } i ? new IngressSpec(i.Subdomain) : null);
        }
        catch (ArgumentException e)
        {
            logger.LogWarning("Rejected app {AppId}: {Reason}", msg.AppId, e.Message);
            // TODO: Add more specific error handling and logging
            await context.Publish(new ProvisioningFailed(msg.AppId, e.Message));
            return;
        }

        logger.LogInformation("Provisioning {App} for customer {Customer}", spec.AppName, spec.CustomerId);

        ProvisioningResult result;
        try
        {
            result = await provisioner.EnsureAppAsync(spec, context.CancellationToken);
        }
        catch (TimeoutException e)
        {
            logger.LogWarning("App {AppId} not ready: {Reason}", msg.AppId, e.Message);
            await context.Publish(new ProvisioningFailed(msg.AppId, e.Message));
            return;
        }


        logger.LogInformation("Provisioned {App} for customer {Customer} with URL {Url} and internal address {InternalAddress}",
            spec.AppName, spec.CustomerId, result.Url, result.InternalAddress);
        await context.Publish(new AppProvisioned(msg.AppId, result.Url, result.InternalAddress));
    }
}
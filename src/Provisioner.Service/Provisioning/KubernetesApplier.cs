using System.Net;
using k8s.Autorest;

namespace Provisioner.Service.Provisioning;

public class KubernetesApplier(ILogger<KubernetesApplier> logger)
{
    // TODO: Add better error handling and logging. Atm there is no actuall distinction between errors
    // Probbably will do after having some other microservices depending on correct feedback
    public async Task CreateOrIgnoreConflictAsync<T>(string kind, string name, Func<Task<T>> create)
    {
        try
        {
            await create();
            logger.LogInformation("Created {Kind} {Name}", kind, name);
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogInformation("{Kind} {Name} already exists", kind, name);
        }
    }
}
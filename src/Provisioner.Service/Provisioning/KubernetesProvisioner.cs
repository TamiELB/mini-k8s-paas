using k8s;
using k8s.Models;
using Provisioner.Service.Provisioning.Manifests;

namespace Provisioner.Service.Provisioning;

public class KubernetesProvisioner(IKubernetes kubernetes, KubernetesApplier applier) : IKubernetesProvisioner
{
    // TODO: Make these configurable instead of hardcoding them
    private const string BaseDomain = "localhost";
    private const int PublicPort = 8080;
    // --------------------------------------------------------------
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public async Task<ProvisioningResult> EnsureAppAsync(AppSpec spec, CancellationToken ct)
    {
        var ns = spec.Namespace;
        var appName = spec.AppName;

        await EnsureCustomerAsync(ns, spec.CustomerId, ct);

        if (spec.Workload.Secrets.Count > 0)
        {
            var secret = AppManifests.Secret(appName, spec.Workload.Secrets);
            await applier.CreateOrIgnoreConflictAsync("Secret", secret.Metadata.Name,
                () => kubernetes.CoreV1.CreateNamespacedSecretAsync(secret, ns, cancellationToken: ct));
        }

        var deployment = AppManifests.Deployment(appName, spec.Workload);
        await applier.CreateOrIgnoreConflictAsync("Deployment", appName,
            () => kubernetes.AppsV1.CreateNamespacedDeploymentAsync(deployment, ns, cancellationToken: ct));

        if (spec.Service is { } serviceSpec)
        {
            var service = AppManifests.Service(appName, serviceSpec);
            await applier.CreateOrIgnoreConflictAsync("Service", appName,
                () => kubernetes.CoreV1.CreateNamespacedServiceAsync(service, ns, cancellationToken: ct));
        }

        if (spec is { Ingress: { } ingressSpec, Service: { } ingressService })
        {
            // TODO: Research how domain gets handled and validate if this is consistent
            var host = $"{ingressSpec.Subdomain ?? appName}.{spec.CustomerId}.{BaseDomain}";
            var ingress = AppManifests.Ingress(appName, host, ingressService);
            await applier.CreateOrIgnoreConflictAsync("Ingress", appName,
                () => kubernetes.NetworkingV1.CreateNamespacedIngressAsync(ingress, ns, cancellationToken: ct));
        }

        await WaitUntilReadyAsync(appName, ns, ReadyTimeout, ct);

        var internalAddress = spec.Service is null ? null : await ReadInternalAddressAsync(appName, ns, ct);
        var url = spec.Ingress is null ? null : await ReadUrlAsync(appName, ns, ct);

        return new ProvisioningResult(url, internalAddress);
    }

    private async Task EnsureCustomerAsync(string ns, string customerId, CancellationToken ct)
    {
        var namespaceManifest = CustomerManifests.Namespace(ns, customerId);
        await applier.CreateOrIgnoreConflictAsync("Namespace", ns,
            () => kubernetes.CoreV1.CreateNamespaceAsync(namespaceManifest, cancellationToken: ct));

        V1NetworkPolicy[] policies =
        [
            CustomerManifests.DefaultDenyIngress(),
            CustomerManifests.AllowNamespaceAndIngressController()
        ];

        foreach (var policy in policies)
        {
            await applier.CreateOrIgnoreConflictAsync("NetworkPolicy", policy.Metadata.Name,
                () => kubernetes.NetworkingV1.CreateNamespacedNetworkPolicyAsync(policy, ns, cancellationToken: ct));
        }
    }

    private async Task WaitUntilReadyAsync(string name, string ns, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            while (true)
            {
                var deployment = await kubernetes.AppsV1.ReadNamespacedDeploymentAsync(name, ns, cancellationToken: cts.Token);
                var desired = deployment.Spec.Replicas ?? 1;
                var ready = deployment.Status?.ReadyReplicas ?? 0;

                if (ready >= desired)
                    return;

                await Task.Delay(PollInterval, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Deployment {name} in {ns} was not ready within {timeout}.");
        }
    }

    private async Task<string> ReadInternalAddressAsync(string name, string ns, CancellationToken ct)
    {
        var service = await kubernetes.CoreV1.ReadNamespacedServiceAsync(name, ns, cancellationToken: ct);
        var port = service.Spec.Ports[0].Port;
        return $"{service.Metadata.Name}.{ns}.svc.cluster.local:{port}";
    }

    private async Task<string> ReadUrlAsync(string name, string ns, CancellationToken ct)
    {
        var ingress = await kubernetes.NetworkingV1.ReadNamespacedIngressAsync(name, ns, cancellationToken: ct);
        var host = ingress.Spec.Rules[0].Host;
        var scheme = ingress.Spec.Tls is { Count: > 0 } ? "https" : "http";
        return $"{scheme}://{host}:{PublicPort}";
    }
}
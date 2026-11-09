using k8s;
using Provisioner.Service.Provisioning.Manifests;

namespace Provisioner.Service.Provisioning;

public class KubernetesProvisioner(IKubernetes kubernetes, KubernetesApplier applier) : IKubernetesProvisioner
{
    private const string baseDomain = "localhost";
    public async Task EnsureAppAsync(AppSpec spec, CancellationToken ct)
    {
        await EnsureCustomerAsync(spec.CustomerId, spec.Namespace, ct);

        var ns = spec.Namespace;
        var appName = spec.AppName;

        if (spec.Workload.Secrets.Count > 0)
        {
            var secretBody = AppManifests.Secret(appName, spec.Workload);
            await applier.CreateOrIgnoreConflictAsync("Secret", secretBody.Metadata.Name,
                () => kubernetes.CoreV1.CreateNamespacedSecretAsync(secretBody, ns, cancellationToken: ct));
        }

        var deploymentBody = AppManifests.Deployment(appName, spec.Workload);
        await applier.CreateOrIgnoreConflictAsync("Deployment", appName,
            () => kubernetes.AppsV1.CreateNamespacedDeploymentAsync(deploymentBody, ns, cancellationToken: ct));

        if (spec.Service is { } serviceSpec)
        {
            var serviceBody = AppManifests.Service(appName, serviceSpec);
            await applier.CreateOrIgnoreConflictAsync("Service", appName,
                () => kubernetes.CoreV1.CreateNamespacedServiceAsync(serviceBody, ns, cancellationToken: ct));

            if (spec.Ingress is { } ingressSpec)
            {
                var host = $"{ingressSpec.Subdomain ?? $"{appName}.{spec.CustomerId}.{baseDomain}"}";
                var ingressBody = AppManifests.Ingress(appName, host, serviceSpec);
                await applier.CreateOrIgnoreConflictAsync("Ingress", host,
                    () => kubernetes.NetworkingV1.CreateNamespacedIngressAsync(ingressBody, ns, cancellationToken: ct));
            }
        }
    }

    private async Task EnsureCustomerAsync(string customerId, string ns, CancellationToken ct)
    {
        var namespaceBody = CustomerManifests.Namespace(ns, customerId);
        await applier.CreateOrIgnoreConflictAsync("Namespace", ns,
            () => kubernetes.CoreV1.CreateNamespaceAsync(namespaceBody, cancellationToken: ct));

        var denyBody = CustomerManifests.DefaultDenyIngress();
        await applier.CreateOrIgnoreConflictAsync("NetworkPolicy", denyBody.Metadata.Name,
            () => kubernetes.NetworkingV1.CreateNamespacedNetworkPolicyAsync(denyBody, ns, cancellationToken: ct));

        var allowBody = CustomerManifests.AllowNamespaceAndIngressController();
        await applier.CreateOrIgnoreConflictAsync("NetworkPolicy", allowBody.Metadata.Name,
            () => kubernetes.NetworkingV1.CreateNamespacedNetworkPolicyAsync(allowBody, ns, cancellationToken: ct));
    }
}
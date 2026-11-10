namespace Provisioner.Service.Provisioning;
public interface IKubernetesProvisioner
            {
    Task<ProvisioningResult> EnsureAppAsync(AppSpec appSpec, CancellationToken ct);
}
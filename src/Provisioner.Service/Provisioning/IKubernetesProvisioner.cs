namespace Provisioner.Service.Provisioning;
public interface IKubernetesProvisioner
            {
    Task EnsureAppAsync(AppSpec appSpec, CancellationToken ct);
}
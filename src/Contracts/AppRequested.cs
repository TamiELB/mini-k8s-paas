namespace Contracts;

public record AppRequested(
    Guid AppId,
    string CustomerId,
    string AppName,
    WorkloadRequest Workload,
    ServiceRequest? Service = null,
    IngressRequest? Ingress = null);

// TODO: (SECURITY) Design proper flow since secrets should not be send in event and as plain text.

public record WorkloadRequest(string Image, int Replicas = 1, IReadOnlyDictionary<string, string>? Env = null,
    IReadOnlyDictionary<string, string>? Secrets = null);

public record ServiceRequest(int Port, int? TargetPort = null);

public record IngressRequest(string? Subdomain = null);

public record ProvisioningFailed(Guid AppId, string Reason);
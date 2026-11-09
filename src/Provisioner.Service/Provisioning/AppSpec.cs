namespace Provisioner.Service.Provisioning;

public record WorkloadSpec
{
    public string Image { get; }
    public int Replicas { get; }
    public IReadOnlyDictionary<string, string> Env { get; }
    public IReadOnlyDictionary<string, string> Secrets { get; }

    public WorkloadSpec(
        string image,
        int replicas = 1,
        IReadOnlyDictionary<string, string>? env = null,
        IReadOnlyDictionary<string, string>? secrets = null)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new ArgumentException("Image is required.");
        if (replicas < 0)
            throw new ArgumentException("Replicas cannot be negative.");

        var envCopy = env?.ToDictionary() ?? [];
        var secretsCopy = secrets?.ToDictionary() ?? [];

        if (envCopy.Keys.Concat(secretsCopy.Keys).Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Variable names cannot be empty.");
        if (envCopy.Keys.Intersect(secretsCopy.Keys).FirstOrDefault() is { } duplicate)
            throw new ArgumentException($"'{duplicate}' is defined in both Env and Secrets.");

        Image = image;
        Replicas = replicas;
        Env = envCopy;
        Secrets = secretsCopy;
    }
}

public record ServiceSpec
{
    public int Port { get; }
    public int TargetPort { get; }

    public ServiceSpec(int port, int? targetPort = null)
    {
        if (!IsValidPort(port))
            throw new ArgumentException($"Port {port} must be between 1 and 65535.");
        if (targetPort is { } t && !IsValidPort(t))
            throw new ArgumentException($"TargetPort {t} must be between 1 and 65535.");

        Port = port;
        TargetPort = targetPort ?? port;
    }

    private static bool IsValidPort(int port) => port is >= 1 and <= 65535;
}

public record IngressSpec(string? Subdomain = null);

public record AppSpec
{
    public string CustomerId { get; }
    public string AppName { get; }
    public WorkloadSpec Workload { get; }
    public ServiceSpec? Service { get; }
    public IngressSpec? Ingress { get; }

    public AppSpec(string customerId, string appName, WorkloadSpec workload,
        ServiceSpec? service = null, IngressSpec? ingress = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("AppName is required.");
        if (ingress is not null && service is null)
            throw new ArgumentException("An Ingress needs a Service.");

        CustomerId = customerId;
        AppName = appName;
        Workload = workload;
        Service = service;
        Ingress = ingress;
    }

    public string Namespace => $"customer-{CustomerId}";
}
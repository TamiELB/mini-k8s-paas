using System.ComponentModel.DataAnnotations;

namespace Portal.Api.Endpoints.Dtos;

public sealed record CreateAppRequest : IValidatableObject
{
    [Required, RegularExpression(NamePatterns.DnsLabel), MaxLength(54)]
    public required string CustomerId { get; init; }

    [Required, RegularExpression(NamePatterns.ServiceName), MaxLength(63)]
    public required string AppName { get; init; }

    [Required]
    public required WorkloadDto Workload { get; init; }

    public ServiceDto? Service { get; init; }

    public IngressDto? Ingress { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ingress is not null && Service is null)
            yield return new ValidationResult("An Ingress needs a Service.", [nameof(Ingress)]);
    }
}

public sealed record WorkloadDto
{
    [Required]
    public required string Image { get; init; }

    [Range(0, 10)]
    public int Replicas { get; init; } = 1;

    public IReadOnlyDictionary<string, string>? Env { get; init; }

    public IReadOnlyDictionary<string, string>? Secrets { get; init; }
}

public sealed record ServiceDto
{
    [Range(1, 65535)]
    public required int Port { get; init; }

    [Range(1, 65535)]
    public int? TargetPort { get; init; }
}

public sealed record IngressDto
{
    [RegularExpression(NamePatterns.DnsLabel), MaxLength(63)]
    public string? Subdomain { get; init; }
}

public static class NamePatterns
{
    public const string DnsLabel = "^[a-z0-9]([-a-z0-9]*[a-z0-9])?$";
    public const string ServiceName = "^[a-z]([-a-z0-9]*[a-z0-9])?$";
}
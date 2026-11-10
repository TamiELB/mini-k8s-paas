namespace Portal.Api.Domain;

public enum AppStatus
{
    Requested,
    Running,
    Failed
}

public class App
{
    public const int CustomerIdMaxLength = 54;
    public const int NameMaxLength = 63;

    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public AppStatus Status { get; private set; }
    public string? Url { get; private set; }
    public string? InternalAdress { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private App() { }

    public static App Request(Guid id, string customerId, string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(customerId) || customerId.Length > CustomerIdMaxLength)
            throw new ArgumentException("Invalid customer id.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
            throw new ArgumentException("Invalid app name.", nameof(name));

        return new App
        {
            Id = id,
            CustomerId = customerId,
            Name = name,
            Status = AppStatus.Requested,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkRunning(string? url, string? InternalAdress, DateTimeOffset now)
    {
        if (Status != AppStatus.Requested)
            return;

        Status = AppStatus.Running;
        Url = url;
        InternalAdress = InternalAdress;
        UpdatedAt = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        if (Status != AppStatus.Requested)
            return;

        Status = AppStatus.Failed;
        FailureReason = reason;
        UpdatedAt = now;
    }
}
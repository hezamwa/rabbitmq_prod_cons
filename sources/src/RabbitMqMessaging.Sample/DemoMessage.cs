namespace WH.SharedKernel.RabbitMqMessaging.Sample;

/// <summary>
/// Represents a sample message contract used by the demo application.
/// </summary>
public sealed class DemoMessage
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}


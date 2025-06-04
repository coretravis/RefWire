namespace ListServDB.Orchestration;

/// <summary>
/// Represents a message between app instances.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    public Guid MessageId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the message.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the app instance sending the message.
    /// </summary>
    public Guid FromAppInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the app instance receiving the message.
    /// </summary>
    public Guid ToAppInstanceId { get; set; }
}

namespace EmailAutomation.Web.Models;

public class EmailJobRecipient
{
    public int Id { get; set; }

    public int JobId { get; set; }
    public int ContactId { get; set; }

    public required string Email { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// Sent | Failed | Ignored
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// For Failed: short failure code (e.g. GraphError). For Ignored: short ignore code (e.g. ContactIgnoredFlag).
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// For Failed: error/exception message. For Ignored: optional human-readable detail.
    /// </summary>
    public string? ReasonMessage { get; set; }

    public int AttemptCount { get; set; }
    public DateTime? FirstAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }

    public EmailJob Job { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}


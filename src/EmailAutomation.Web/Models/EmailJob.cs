namespace EmailAutomation.Web.Models;

/// <summary>
/// Represents a single email send run.
/// </summary>
public class EmailJob
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int? TemplateId { get; set; }
    public required string TemplateSubject { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public required string Status { get; set; } // Running, Completed, Failed
    public int SentCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int? RetryOfJobId { get; set; }

    public Batch Batch { get; set; } = null!;
    public ICollection<EmailJobRecipient> Recipients { get; set; } = [];
}

namespace EmailAutomation.Web.Models;

/// <summary>
/// Tracks an import operation against the Contacts table.
/// </summary>
public class ImportLog
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public DateTime ImportedAt { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorsJson { get; set; }
}


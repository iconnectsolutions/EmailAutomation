namespace EmailAutomation.Web.Models;

/// <summary>
/// Represents a single CSV import operation.
/// </summary>
public class ImportBatch
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public DateTime ImportedAt { get; set; }
    public int RowCount { get; set; }

    public ICollection<Recipient> Recipients { get; set; } = [];
}

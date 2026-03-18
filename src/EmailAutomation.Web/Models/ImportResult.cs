namespace EmailAutomation.Web.Models;

/// <summary>
/// Result of an import operation. Includes counts and errors.
/// </summary>
public class ImportResult
{
    public ImportLog ImportLog { get; set; } = null!;
    public int RowCount { get; set; }
    public int SkippedCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}

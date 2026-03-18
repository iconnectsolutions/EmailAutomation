namespace EmailAutomation.Web.Models;

/// <summary>
/// User-defined group of contacts used as a send target.
/// </summary>
public class Batch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<BatchContact> BatchContacts { get; set; } = [];
}


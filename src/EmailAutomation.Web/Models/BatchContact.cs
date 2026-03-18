namespace EmailAutomation.Web.Models;

/// <summary>
/// Join entity between Batch and Contact.
/// </summary>
public class BatchContact
{
    public int BatchId { get; set; }
    public int ContactId { get; set; }

    public Batch Batch { get; set; } = null!;
    public Contact Contact { get; set; } = null!;
}


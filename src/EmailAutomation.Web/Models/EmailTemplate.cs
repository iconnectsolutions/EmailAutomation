namespace EmailAutomation.Web.Models;

/// <summary>
/// User-defined email template with name, subject, and body.
/// Supports {FirstName} placeholder for personalization.
/// </summary>
public class EmailTemplate
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; }
}

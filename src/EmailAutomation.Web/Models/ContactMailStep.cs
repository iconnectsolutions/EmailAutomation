namespace EmailAutomation.Web.Models;

public class ContactMailStep
{
    public int Id { get; set; }

    public int ContactId { get; set; }
    public int StepNumber { get; set; } // 1..N
    public DateTime SentAt { get; set; }

    public Contact Contact { get; set; } = null!;
}


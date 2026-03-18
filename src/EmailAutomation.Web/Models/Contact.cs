namespace EmailAutomation.Web.Models;

/// <summary>
/// Central contact record, de-duplicated by email.
/// Tracks global mail send dates and ignore flag.
/// </summary>
public class Contact
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime? Mail1Date { get; set; }
    public DateTime? Mail2Date { get; set; }
    public DateTime? Mail3Date { get; set; }
    public DateTime? Mail4Date { get; set; }
    public DateTime? Mail5Date { get; set; }
    public bool Ignore { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Returns the first null mail date column index (1-based). Returns 0 if all are filled.
    /// </summary>
    public int GetNextMailColumnIndex()
    {
        if (Mail1Date == null) return 1;
        if (Mail2Date == null) return 2;
        if (Mail3Date == null) return 3;
        if (Mail4Date == null) return 4;
        if (Mail5Date == null) return 5;
        return 0;
    }

    /// <summary>
    /// Sets the mail date for the given column index (1-based).
    /// </summary>
    public void SetMailDate(int columnIndex, DateTime date)
    {
        switch (columnIndex)
        {
            case 1: Mail1Date = date; break;
            case 2: Mail2Date = date; break;
            case 3: Mail3Date = date; break;
            case 4: Mail4Date = date; break;
            case 5: Mail5Date = date; break;
        }
    }
}


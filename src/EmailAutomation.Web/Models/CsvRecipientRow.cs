using CsvHelper.Configuration.Attributes;

namespace EmailAutomation.Web.Models;

/// <summary>
/// Maps CSV columns (case-insensitive) to recipient fields.
/// </summary>
public class CsvRecipientRow
{
    [Name("email")]
    [Optional]
    public string? Email { get; set; }

    [Name("name")]
    [Optional]
    public string? Name { get; set; }

    [Name("mail1")]
    [Optional]
    public string? Mail1 { get; set; }

    [Name("mail2")]
    [Optional]
    public string? Mail2 { get; set; }

    [Name("mail3")]
    [Optional]
    public string? Mail3 { get; set; }

    [Name("mail4")]
    [Optional]
    public string? Mail4 { get; set; }

    [Name("mail5")]
    [Optional]
    public string? Mail5 { get; set; }

    [Name("ignore")]
    [Optional]
    public string? Ignore { get; set; }
}

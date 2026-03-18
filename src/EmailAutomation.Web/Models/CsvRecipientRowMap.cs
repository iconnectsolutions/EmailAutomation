using CsvHelper.Configuration;

namespace EmailAutomation.Web.Models;

public class CsvRecipientRowMap : ClassMap<CsvRecipientRow>
{
    public CsvRecipientRowMap()
    {
        Map(m => m.Email).Name("email", "Email");
        Map(m => m.Name).Name("name", "Name");
        Map(m => m.Mail1).Name("mail1", "Mail1");
        Map(m => m.Mail2).Name("mail2", "Mail2");
        Map(m => m.Mail3).Name("mail3", "Mail3");
        Map(m => m.Mail4).Name("mail4", "Mail4");
        Map(m => m.Mail5).Name("mail5", "Mail5");
        Map(m => m.Ignore).Name("ignore", "Ignore");
    }
}

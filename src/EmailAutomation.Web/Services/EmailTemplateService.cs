using EmailAutomation.Web.Data;
using EmailAutomation.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly AppDbContext _db;

    public EmailTemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.EmailTemplates
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.EmailTemplates.FindAsync([id], cancellationToken);
    }

    public async Task<EmailTemplate> CreateAsync(string name, string subject, string body, CancellationToken cancellationToken = default)
    {
        var template = new EmailTemplate
        {
            Name = name.Trim(),
            Subject = subject.Trim(),
            Body = body ?? "",
            CreatedAt = DateTime.UtcNow
        };
        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<EmailTemplate?> UpdateAsync(int id, string name, string subject, string body, CancellationToken cancellationToken = default)
    {
        var template = await _db.EmailTemplates.FindAsync([id], cancellationToken);
        if (template == null)
            return null;

        template.Name = name.Trim();
        template.Subject = subject.Trim();
        template.Body = body ?? "";
        await _db.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await _db.EmailTemplates.FindAsync([id], cancellationToken);
        if (template == null)
            return false;

        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

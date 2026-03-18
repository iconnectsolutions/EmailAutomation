using EmailAutomation.Web.Models;

namespace EmailAutomation.Web.Services;

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EmailTemplate> CreateAsync(string name, string subject, string body, CancellationToken cancellationToken = default);
    Task<EmailTemplate?> UpdateAsync(int id, string name, string subject, string body, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

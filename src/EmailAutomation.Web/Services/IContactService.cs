using EmailAutomation.Web.Models;

namespace EmailAutomation.Web.Services;

public interface IContactService
{
    Task<IReadOnlyList<Contact>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken = default);
    Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Contact> CreateAsync(string email, string name, bool ignore, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Contact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task UpdateMailDateAsync(int contactId, int mailColumnIndex, DateTime date, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}


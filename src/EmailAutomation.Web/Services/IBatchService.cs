using EmailAutomation.Web.Models;

namespace EmailAutomation.Web.Services;

public interface IBatchService
{
    Task<IReadOnlyList<Batch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Batch?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetContactsByBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<Batch> CreateAsync(string name, IEnumerable<int> contactIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}


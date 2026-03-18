using EmailAutomation.Web.Data;
using EmailAutomation.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Services;

public class BatchService : IBatchService
{
    private readonly AppDbContext _db;

    public BatchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Batch>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Batches
            .Include(b => b.BatchContacts)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Batch?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Batches.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetContactsByBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _db.BatchContacts
            .Where(bc => bc.BatchId == batchId)
            .Include(bc => bc.Contact)
            .Select(bc => bc.Contact)
            .OrderBy(c => c.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<Batch> CreateAsync(string name, IEnumerable<int> contactIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = contactIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
            throw new InvalidOperationException("At least one contact is required to create a batch.");

        var batch = new Batch
        {
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        var batchContacts = distinctIds.Select(id => new BatchContact
        {
            BatchId = batch.Id,
            ContactId = id
        }).ToList();

        _db.BatchContacts.AddRange(batchContacts);
        await _db.SaveChangesAsync(cancellationToken);

        return batch;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var batch = await _db.Batches.FindAsync([id], cancellationToken);
        if (batch == null)
            return false;

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}


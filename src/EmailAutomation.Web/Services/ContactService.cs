using EmailAutomation.Web.Data;
using EmailAutomation.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Email.ToLower().Contains(term) ||
                c.Name.ToLower().Contains(term));
        }

        query = query
            .OrderBy(c => c.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _db.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Email.ToLower().Contains(term) ||
                c.Name.ToLower().Contains(term));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Contact?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Contacts.FindAsync([id], cancellationToken);
    }

    public async Task<Contact> CreateAsync(string email, string name, bool ignore, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        name = name.Trim();

        var existing = await _db.Contacts.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var contact = new Contact
        {
            Email = email,
            Name = name,
            Ignore = ignore,
            CreatedAt = DateTime.UtcNow
        };

        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);
        return contact;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        return await _db.Contacts.AnyAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<Contact?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        email = email.Trim();
        return await _db.Contacts.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idArray = ids.Distinct().ToArray();
        if (idArray.Length == 0)
            return Array.Empty<Contact>();

        return await _db.Contacts
            .Where(c => idArray.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateMailDateAsync(int contactId, int mailColumnIndex, DateTime date, CancellationToken cancellationToken = default)
    {
        var contact = await _db.Contacts.FindAsync([contactId], cancellationToken);
        if (contact == null)
            return;

        contact.SetMailDate(mailColumnIndex, date);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contact = await _db.Contacts.FindAsync([id], cancellationToken);
        if (contact == null)
            return false;

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}


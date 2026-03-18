using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using EmailAutomation.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _batchService;
    private readonly AppDbContext _db;
    private const int MaxFollowupSteps = 15;

    public BatchesController(IBatchService batchService, AppDbContext db)
    {
        _batchService = batchService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var batches = await _batchService.GetAllAsync(ct);
        return Ok(batches.Select(b => new
        {
            b.Id,
            b.Name,
            b.CreatedAt,
            ContactCount = b.BatchContacts.Count
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var batch = await _batchService.GetByIdAsync(id, ct);
        if (batch == null)
            return NotFound();

        return Ok(new
        {
            batch.Id,
            batch.Name,
            batch.CreatedAt
        });
    }

    [HttpGet("{id}/contacts")]
    public async Task<IActionResult> GetContacts(int id, CancellationToken ct)
    {
        var contacts = await _batchService.GetContactsByBatchAsync(id, ct);
        var ids = contacts.Select(c => c.Id).ToArray();
        var steps = await _db.ContactMailSteps
            .Where(s => ids.Contains(s.ContactId) && s.StepNumber >= 1 && s.StepNumber <= MaxFollowupSteps)
            .Select(s => new { s.ContactId, s.StepNumber, s.SentAt })
            .ToListAsync(ct);

        var map = steps
            .GroupBy(s => s.ContactId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.StepNumber, x => (DateTime?)x.SentAt));

        return Ok(contacts.Select(c => new
        {
            c.Id,
            c.Email,
            c.Name,
            Mail1Date = map.TryGetValue(c.Id, out var s1) && s1.TryGetValue(1, out var d1) ? d1 : null,
            Mail2Date = map.TryGetValue(c.Id, out var s2) && s2.TryGetValue(2, out var d2) ? d2 : null,
            Mail3Date = map.TryGetValue(c.Id, out var s3) && s3.TryGetValue(3, out var d3) ? d3 : null,
            Mail4Date = map.TryGetValue(c.Id, out var s4) && s4.TryGetValue(4, out var d4) ? d4 : null,
            Mail5Date = map.TryGetValue(c.Id, out var s5) && s5.TryGetValue(5, out var d5) ? d5 : null,
            Mail6Date = map.TryGetValue(c.Id, out var s6) && s6.TryGetValue(6, out var d6) ? d6 : null,
            Mail7Date = map.TryGetValue(c.Id, out var s7) && s7.TryGetValue(7, out var d7) ? d7 : null,
            Mail8Date = map.TryGetValue(c.Id, out var s8) && s8.TryGetValue(8, out var d8) ? d8 : null,
            Mail9Date = map.TryGetValue(c.Id, out var s9) && s9.TryGetValue(9, out var d9) ? d9 : null,
            Mail10Date = map.TryGetValue(c.Id, out var s10) && s10.TryGetValue(10, out var d10) ? d10 : null,
            Mail11Date = map.TryGetValue(c.Id, out var s11) && s11.TryGetValue(11, out var d11) ? d11 : null,
            Mail12Date = map.TryGetValue(c.Id, out var s12) && s12.TryGetValue(12, out var d12) ? d12 : null,
            Mail13Date = map.TryGetValue(c.Id, out var s13) && s13.TryGetValue(13, out var d13) ? d13 : null,
            Mail14Date = map.TryGetValue(c.Id, out var s14) && s14.TryGetValue(14, out var d14) ? d14 : null,
            Mail15Date = map.TryGetValue(c.Id, out var s15) && s15.TryGetValue(15, out var d15) ? d15 : null,
            c.Ignore
        }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _batchService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}


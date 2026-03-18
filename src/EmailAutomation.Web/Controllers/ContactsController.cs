using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using EmailAutomation.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly IBatchService _batchService;
    private readonly AppDbContext _db;
    private const int MaxFollowupSteps = 15;

    public ContactsController(IContactService contactService, IBatchService batchService, AppDbContext db)
    {
        _contactService = contactService;
        _batchService = batchService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 200) pageSize = 50;

        var total = await _contactService.CountAsync(search, ct);
        var items = await _contactService.GetAllAsync(search, page, pageSize, ct);

        var ids = items.Select(c => c.Id).ToArray();
        var steps = await _db.ContactMailSteps
            .Where(s => ids.Contains(s.ContactId) && s.StepNumber >= 1 && s.StepNumber <= MaxFollowupSteps)
            .Select(s => new { s.ContactId, s.StepNumber, s.SentAt })
            .ToListAsync(ct);

        var map = steps
            .GroupBy(s => s.ContactId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.StepNumber, x => (DateTime?)x.SentAt));

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = items.Select(c => new
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
                c.Ignore,
                c.CreatedAt
            })
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _contactService.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound(new { error = "Contact not found" });
        return NoContent();
    }

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatchFromContacts([FromBody] CreateBatchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Batch name is required" });

        if (request.ContactIds == null || request.ContactIds.Length == 0)
            return BadRequest(new { error = "At least one contact must be selected" });

        var batch = await _batchService.CreateAsync(request.Name, request.ContactIds, ct);

        return Ok(new
        {
            batch.Id,
            batch.Name,
            batch.CreatedAt
        });
    }
}

public record CreateBatchRequest(string Name, int[] ContactIds);


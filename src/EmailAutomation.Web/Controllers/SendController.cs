using EmailAutomation.Web.Data;
using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SendController : ControllerBase
{
    private readonly IGraphMailService _graphMail;
    private readonly IBatchService _batchService;
    private readonly AppDbContext _db;

    public SendController(IGraphMailService graphMail, IBatchService batchService, AppDbContext db)
    {
        _graphMail = graphMail;
        _batchService = batchService;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendRequest request, CancellationToken ct)
    {
        if (request.TemplateId <= 0)
            return BadRequest(new { error = "Valid template ID is required" });

        if (request.BatchId <= 0)
            return BadRequest(new { error = "Valid batch ID is required" });

        var batch = await _batchService.GetByIdAsync(request.BatchId, ct);
        if (batch == null)
            return NotFound(new { error = "Batch not found" });

        try
        {
            var job = await _graphMail.SendBatchEmailsAsync(request.BatchId, request.TemplateId, null, null, null, ct);
            return Ok(new
            {
                job.Id,
                job.Status,
                job.SentCount,
                job.CompletedAt,
                job.ErrorMessage
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new { error = "Database error while saving email job.", detail = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Unexpected error while sending emails.", detail = ex.Message });
        }
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs(CancellationToken ct)
    {
        var jobs = await _db.EmailJobs
            .OrderByDescending(j => j.StartedAt)
            .Take(50)
            .Select(j => new
            {
                j.Id,
                j.BatchId,
                j.TemplateId,
                j.TemplateSubject,
                j.StartedAt,
                j.CompletedAt,
                j.Status,
                j.SentCount,
                j.ErrorMessage,
                j.RetryOfJobId
            })
            .ToListAsync(ct);

        return Ok(jobs);
    }

    [HttpGet("jobs/{jobId:int}/recipients")]
    public async Task<IActionResult> GetJobRecipients(int jobId, [FromQuery] string? status, CancellationToken ct)
    {
        var query = _db.EmailJobRecipients
            .Where(r => r.JobId == jobId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var recipients = await query
            .OrderBy(r => r.Email)
            .Select(r => new
            {
                r.Id,
                r.JobId,
                r.ContactId,
                r.Email,
                r.Name,
                r.Status,
                r.ReasonCode,
                r.ReasonMessage,
                r.AttemptCount,
                r.FirstAttemptAt,
                r.LastAttemptAt
            })
            .ToListAsync(ct);

        return Ok(recipients);
    }

    [HttpPost("jobs/{jobId:int}/retry-failed")]
    public async Task<IActionResult> RetryFailedRecipients(int jobId, CancellationToken ct)
    {
        var sourceJob = await _db.EmailJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (sourceJob == null)
            return NotFound(new { error = "Job not found" });

        if (sourceJob.TemplateId == null)
            return BadRequest(new { error = "This job does not have a TemplateId stored; retry is not supported for this historical job." });

        // Only retry recipients that actually failed in the source job.
        var failedContactIds = await _db.EmailJobRecipients
            .Where(r => r.JobId == jobId && r.Status == "Failed")
            .Select(r => r.ContactId)
            .Distinct()
            .ToListAsync(ct);

        if (failedContactIds.Count == 0)
            return Ok(new { message = "No failed recipients to retry." });

        // Create a new job and send only to failed recipients by temporarily filtering in memory.
        // We reuse the same batch + template; GraphMailService will log recipients for the new job.
        // Note: This assumes failed recipients are still part of the batch and not ignored globally.
        var newJob = await _graphMail.SendBatchEmailsAsync(
            sourceJob.BatchId,
            sourceJob.TemplateId.Value,
            failedContactIds,
            sourceJob.Id,
            null,
            ct);

        return Ok(new
        {
            newJob.Id,
            newJob.Status,
            newJob.SentCount,
            newJob.CompletedAt,
            newJob.ErrorMessage,
            newJob.RetryOfJobId
        });
    }
}

public record SendRequest(int BatchId, int TemplateId);

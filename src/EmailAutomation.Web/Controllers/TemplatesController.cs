using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly IEmailTemplateService _templateService;

    public TemplatesController(IEmailTemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>Get all email templates.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var templates = await _templateService.GetAllAsync(ct);
        return Ok(templates.Select(t => new { t.Id, t.Name, t.Subject, t.Body, t.CreatedAt }));
    }

    /// <summary>Get a template by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var template = await _templateService.GetByIdAsync(id, ct);
        if (template == null)
            return NotFound(new { error = "Template not found" });
        return Ok(new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    /// <summary>Create a new template.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });
        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { error = "Subject is required" });

        var template = await _templateService.CreateAsync(
            request.Name,
            request.Subject,
            request.Body ?? "",
            ct);
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    /// <summary>Update an existing template.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });
        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { error = "Subject is required" });

        var template = await _templateService.UpdateAsync(id, request.Name, request.Subject, request.Body ?? "", ct);
        if (template == null)
            return NotFound(new { error = "Template not found" });
        return Ok(new { template.Id, template.Name, template.Subject, template.Body, template.CreatedAt });
    }

    /// <summary>Delete a template.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await _templateService.DeleteAsync(id, ct))
            return NotFound(new { error = "Template not found" });
        return NoContent();
    }
}

public record CreateTemplateRequest(string Name, string Subject, string? Body);

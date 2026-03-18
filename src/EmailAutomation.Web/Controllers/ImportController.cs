using EmailAutomation.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailAutomation.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IRecipientService _recipientService;

    public ImportController(IRecipientService recipientService)
    {
        _recipientService = recipientService;
    }

    [HttpPost("csv")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var fileName = file.FileName;
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be a CSV" });

        await using var stream = file.OpenReadStream();
        var result = await _recipientService.ImportCsvAsync(stream, fileName, ct);

        return Ok(new
        {
            fileName = result.ImportLog.FileName,
            rowCount = result.RowCount,
            skippedCount = result.SkippedCount,
            errors = result.Errors,
            importedAt = result.ImportLog.ImportedAt
        });
    }

    [HttpPost("excel")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        var fileName = file.FileName;
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be an Excel file (.xlsx)" });

        await using var stream = file.OpenReadStream();
        var result = await _recipientService.ImportExcelAsync(stream, fileName, ct);

        return Ok(new
        {
            fileName = result.ImportLog.FileName,
            rowCount = result.RowCount,
            skippedCount = result.SkippedCount,
            errors = result.Errors,
            importedAt = result.ImportLog.ImportedAt
        });
    }
}

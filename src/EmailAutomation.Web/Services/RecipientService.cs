using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using EmailAutomation.Web.Data;
using EmailAutomation.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Services;

    public class RecipientService : IRecipientService
{
    private readonly AppDbContext _db;
    private readonly IContactService _contactService;

    public RecipientService(AppDbContext db, IContactService contactService)
    {
        _db = db;
        _contactService = contactService;
    }

    public async Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
            BadDataFound = null
        };

        var errors = new List<string>();
        var addedContacts = new List<Contact>();

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<CsvRecipientRowMap>();

        var rowIndex = 1;
        IEnumerable<CsvRecipientRow> records;
        try
        {
            records = csv.GetRecords<CsvRecipientRow>();
        }
        catch (Exception ex)
        {
            var logError = new ImportLog
            {
                FileName = fileName,
                ImportedAt = DateTime.UtcNow,
                AddedCount = 0,
                SkippedCount = 1,
                ErrorsJson = System.Text.Json.JsonSerializer.Serialize(new[] { ex.Message })
            };

            _db.ImportLogs.Add(logError);
            await _db.SaveChangesAsync(cancellationToken);

            return new ImportResult
            {
                ImportLog = logError,
                RowCount = 0,
                SkippedCount = 1,
                Errors = new[] { ex.Message }
            };
        }

        foreach (var row in records)
        {
            rowIndex++;
            try
            {
                if (string.IsNullOrWhiteSpace(row?.Email))
                {
                    errors.Add($"Row {rowIndex}: Skipped (no email)");
                    continue;
                }

                var email = row.Email.Trim();
                if (!IsValidEmail(email))
                {
                    errors.Add($"Row {rowIndex}: Invalid email '{email}'");
                    continue;
                }

                if (await _contactService.ExistsByEmailAsync(email, cancellationToken))
                {
                    errors.Add($"Row {rowIndex}: Skipped (email already exists)");
                    continue;
                }

                var ignore = IsIgnore(row.Ignore);
                var contact = await _contactService.CreateAsync(email, (row.Name ?? "").Trim(), ignore, cancellationToken);
                addedContacts.Add(contact);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowIndex}: {ex.Message}");
            }
        }

        var log = new ImportLog
        {
            FileName = fileName,
            ImportedAt = DateTime.UtcNow,
            AddedCount = addedContacts.Count,
            SkippedCount = errors.Count,
            ErrorsJson = errors.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(errors) : null
        };

        _db.ImportLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return new ImportResult
        {
            ImportLog = log,
            RowCount = addedContacts.Count,
            SkippedCount = errors.Count,
            Errors = errors
        };
    }

    public async Task<ImportResult> ImportExcelAsync(Stream excelStream, string fileName, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var addedContacts = new List<Contact>();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.RowsUsed().Any()) ?? workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        if (lastRow < 2)
        {
            var logEmpty = new ImportLog
            {
                FileName = fileName,
                ImportedAt = DateTime.UtcNow,
                AddedCount = 0,
                SkippedCount = 0,
                ErrorsJson = System.Text.Json.JsonSerializer.Serialize(new[] { "No data rows found in Excel file" })
            };
            _db.ImportLogs.Add(logEmpty);
            await _db.SaveChangesAsync(cancellationToken);

            return new ImportResult
            {
                ImportLog = logEmpty,
                RowCount = 0,
                SkippedCount = 0,
                Errors = ["No data rows found in Excel file"]
            };
        }

        var headerRow = worksheet.FirstRow();
        var colMap = BuildColumnMap(headerRow);

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            try
            {
                var email = GetCellValue(worksheet, rowNum, colMap, "email");
                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add($"Row {rowNum}: Skipped (no email)");
                    continue;
                }

                email = email.Trim();
                if (!IsValidEmail(email))
                {
                    errors.Add($"Row {rowNum}: Invalid email '{email}'");
                    continue;
                }

                if (await _contactService.ExistsByEmailAsync(email, cancellationToken))
                {
                    errors.Add($"Row {rowNum}: Skipped (email already exists)");
                    continue;
                }

                var name = GetCellValue(worksheet, rowNum, colMap, "name") ?? "";
                var ignoreVal = GetCellValue(worksheet, rowNum, colMap, "ignore");
                var ignore = IsIgnore(ignoreVal);

                var contact = await _contactService.CreateAsync(email, name.Trim(), ignore, cancellationToken);
                addedContacts.Add(contact);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowNum}: {ex.Message}");
            }
        }

        var log = new ImportLog
        {
            FileName = fileName,
            ImportedAt = DateTime.UtcNow,
            AddedCount = addedContacts.Count,
            SkippedCount = errors.Count,
            ErrorsJson = errors.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(errors) : null
        };

        _db.ImportLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return new ImportResult
        {
            ImportLog = log,
            RowCount = addedContacts.Count,
            SkippedCount = errors.Count,
            Errors = errors
        };
    }

    private static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString()?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(header))
                map[header] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static string? GetCellValue(IXLWorksheet ws, int row, IReadOnlyDictionary<string, int> colMap, string columnName)
    {
        if (!colMap.TryGetValue(columnName, out var col))
            return null;
        var cell = ws.Cell(row, col);
        var val = cell.GetString();
        if (string.IsNullOrEmpty(val) && cell.TryGetValue(out DateTime dt))
            return dt.ToString("yyyy-MM-dd");
        return val;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            return false;
        return email.Contains('@') && email.IndexOf('@') > 0 && email.LastIndexOf('@') < email.Length - 1;
    }

    public async Task<ImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _db.ImportBatches
            .Include(b => b.Recipients)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportBatch>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ImportBatches
            .OrderByDescending(b => b.ImportedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recipient>> GetRecipientsByBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _db.Recipients
            .Where(r => r.BatchId == batchId)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Recipient?> GetRecipientAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Recipients.FindAsync([id], cancellationToken);
    }

    public async Task UpdateRecipientMailDateAsync(int recipientId, int mailColumnIndex, DateTime date, CancellationToken cancellationToken = default)
    {
        var recipient = await _db.Recipients.FindAsync([recipientId], cancellationToken);
        if (recipient == null)
            return;

        recipient.SetMailDate(mailColumnIndex, date);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsIgnore(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var v = value.Trim().ToLowerInvariant();
        return v is "yes" or "true" or "1" or "y";
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}

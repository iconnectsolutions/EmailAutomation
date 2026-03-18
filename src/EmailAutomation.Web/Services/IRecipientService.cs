using EmailAutomation.Web.Models;

namespace EmailAutomation.Web.Services;

public interface IRecipientService
{
    Task<ImportResult> ImportCsvAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default);
    Task<ImportResult> ImportExcelAsync(Stream excelStream, string fileName, CancellationToken cancellationToken = default);
    Task<ImportBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetBatchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Recipient>> GetRecipientsByBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<Recipient?> GetRecipientAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateRecipientMailDateAsync(int recipientId, int mailColumnIndex, DateTime date, CancellationToken cancellationToken = default);
}

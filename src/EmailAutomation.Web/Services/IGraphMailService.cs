using EmailAutomation.Web.Models;

namespace EmailAutomation.Web.Services;

public interface IGraphMailService
{
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    string GetLoginUrl(string redirectUri);
    Task<bool> AcquireTokenByCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);
    Task<EmailJob> SendBatchEmailsAsync(
        int batchId,
        int templateId,
        IReadOnlyCollection<int>? onlyContactIds = null,
        int? retryOfJobId = null,
        IProgress<SendProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record SendProgress(int Total, int Sent, int Skipped, string? CurrentRecipient);

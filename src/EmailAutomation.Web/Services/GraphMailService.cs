using EmailAutomation.Web.Data;
using EmailAutomation.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Services;

public class GraphMailService : IGraphMailService
{
    private const string CacheKey = "GraphToken";
    private const int MaxFollowupSteps = 15;
    private readonly IConfiguration _config;
    private readonly ILogger<GraphMailService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IBatchService _batchService;
    private readonly IContactService _contactService;
    private readonly IEmailTemplateService _templateService;
    private readonly AppDbContext _db;

    private static readonly string[] Scopes = ["User.Read", "Mail.Read", "Mail.Send", "offline_access"];

    public GraphMailService(
        IConfiguration config,
        ILogger<GraphMailService> logger,
        IMemoryCache cache,
        IBatchService batchService,
        IContactService contactService,
        IEmailTemplateService templateService,
        AppDbContext db)
    {
        _config = config;
        _logger = logger;
        _cache = cache;
        _batchService = batchService;
        _contactService = contactService;
        _templateService = templateService;
        _db = db;
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken);
        return token != null;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey);
        _cache.Remove("GraphRefreshToken");
        return Task.CompletedTask;
    }

    public string GetLoginUrl(string redirectUri)
    {
        var clientId = _config["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
        var tenantId = _config["AzureAd:TenantId"] ?? "common";
        var scopes = string.Join("%20", Scopes);
        return $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scopes}&response_mode=query";
    }

    public async Task<bool> AcquireTokenByCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var app = BuildConfidentialClient(redirectUri);
        var result = await app.AcquireTokenByAuthorizationCode(Scopes, code)
            .ExecuteAsync(cancellationToken);

        if (result == null)
            return false;

        _cache.Set(CacheKey, result.AccessToken, TimeSpan.FromMinutes(55));
        return true;
    }

    public async Task<EmailJob> SendBatchEmailsAsync(
        int batchId,
        int templateId,
        IReadOnlyCollection<int>? onlyContactIds = null,
        int? retryOfJobId = null,
        IProgress<SendProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var graphClient = await GetGraphClientAsync(cancellationToken);
        if (graphClient == null)
            throw new InvalidOperationException("Not authenticated. Please connect Outlook first.");

        var batch = await _batchService.GetByIdAsync(batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Batch {batchId} not found.");

        var contacts = await _batchService.GetContactsByBatchAsync(batchId, cancellationToken);
        var toSend = contacts
            .Where(c => onlyContactIds == null || onlyContactIds.Contains(c.Id))
            .ToList();
        var total = toSend.Count;

        var template = await _templateService.GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found.");
        }

        var job = new EmailJob
        {
            BatchId = batchId,
            TemplateId = templateId,
            TemplateSubject = template.Name,
            StartedAt = DateTime.UtcNow,
            Status = "Running",
            SentCount = 0,
            RetryOfJobId = retryOfJobId
        };

        _db.EmailJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        var sent = 0;
        var skipped = 0;
        var templateSubjectText = template.Subject;
        var templateBody = template.Body ?? "";

        try
        {

            foreach (var contact in toSend)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var recipientRow = await _db.EmailJobRecipients
                    .FirstOrDefaultAsync(r => r.JobId == job.Id && r.ContactId == contact.Id, cancellationToken);
                if (recipientRow == null)
                {
                    recipientRow = new EmailJobRecipient
                    {
                        JobId = job.Id,
                        ContactId = contact.Id,
                        Email = contact.Email,
                        Name = contact.Name,
                        Status = "Ignored",
                        AttemptCount = 0
                    };
                    _db.EmailJobRecipients.Add(recipientRow);
                }

                if (contact.Ignore)
                {
                    skipped++;
                    recipientRow.Status = "Ignored";
                    recipientRow.ReasonCode = "ContactIgnoredFlag";
                    recipientRow.ReasonMessage = null;
                    await _db.SaveChangesAsync(cancellationToken);
                    progress?.Report(new SendProgress(total, sent, skipped, $"{contact.Email} (ignored)"));
                    continue;
                }

                var lastStep = await _db.ContactMailSteps
                    .Where(s => s.ContactId == contact.Id)
                    .Select(s => (int?)s.StepNumber)
                    .MaxAsync(cancellationToken) ?? 0;
                var nextStep = lastStep + 1;

                if (nextStep > MaxFollowupSteps)
                {
                    skipped++;
                    recipientRow.Status = "Ignored";
                    recipientRow.ReasonCode = "MailSlotsFull";
                    recipientRow.ReasonMessage = $"Max follow-ups reached ({MaxFollowupSteps})";
                    await _db.SaveChangesAsync(cancellationToken);
                    progress?.Report(new SendProgress(total, sent, skipped, $"{contact.Email} (follow-up slots full)"));
                    continue;
                }

                var body = templateBody.Replace("{FirstName}", contact.Name, StringComparison.OrdinalIgnoreCase);
                var subject = templateSubjectText.Replace("{FirstName}", contact.Name, StringComparison.OrdinalIgnoreCase);

                try
                {
                    recipientRow.AttemptCount += 1;
                    recipientRow.FirstAttemptAt ??= DateTime.UtcNow;
                    recipientRow.LastAttemptAt = DateTime.UtcNow;
                    recipientRow.ReasonCode = null;
                    recipientRow.ReasonMessage = null;
                    await _db.SaveChangesAsync(cancellationToken);

                    var message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = body
                        },
                        ToRecipients =
                        [
                            new Microsoft.Graph.Models.Recipient
                            {
                                EmailAddress = new EmailAddress
                                {
                                    Address = contact.Email,
                                    Name = contact.Name
                                }
                            }
                        ]
                    };

                    await graphClient.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    }, cancellationToken: cancellationToken);

                    var sentAt = DateTime.UtcNow;
                    _db.ContactMailSteps.Add(new ContactMailStep
                    {
                        ContactId = contact.Id,
                        StepNumber = nextStep,
                        SentAt = sentAt
                    });
                    await _db.SaveChangesAsync(cancellationToken);

                    sent++;
                    recipientRow.Status = "Sent";
                    recipientRow.ReasonCode = null;
                    recipientRow.ReasonMessage = null;
                    await _db.SaveChangesAsync(cancellationToken);
                    progress?.Report(new SendProgress(total, sent, skipped, contact.Email));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send to {Email}", contact.Email);
                    skipped++;
                    recipientRow.Status = "Failed";
                    recipientRow.ReasonCode = "GraphError";
                    recipientRow.ReasonMessage = ex.Message;
                    recipientRow.AttemptCount += 1;
                    recipientRow.FirstAttemptAt ??= DateTime.UtcNow;
                    recipientRow.LastAttemptAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                    progress?.Report(new SendProgress(total, sent, skipped, $"{contact.Email} (error: {ex.Message})"));
                }
            }

            job.Status = "Completed";
            job.SentCount = sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send batch failed");
            job.Status = "Failed";
            job.ErrorMessage = ex.Message;
        }
        finally
        {
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return job;
    }

    private Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out string? token) && !string.IsNullOrEmpty(token))
            return Task.FromResult<string?>(token);

        // Token refresh could be added via IByRefreshToken.AcquireTokenByRefreshToken
        // For now, user reconnects when token expires (~55 min)
        return Task.FromResult<string?>(null);
    }

    private async Task<GraphServiceClient?> GetGraphClientAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
            return null;

        var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(token));
        return new GraphServiceClient(authProvider);
    }

    private IConfidentialClientApplication BuildConfidentialClient(string? redirectUri = null)
    {
        var clientId = _config["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
        var tenantId = _config["AzureAd:TenantId"] ?? "common";
        var clientSecret = _config["AzureAd:ClientSecret"] ?? throw new InvalidOperationException("AzureAd:ClientSecret not configured");

        var builder = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{tenantId}");

        if (!string.IsNullOrEmpty(redirectUri))
            builder = builder.WithRedirectUri(redirectUri);

        return builder.Build();
    }

    private class TokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            => Task.FromResult(token);
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}

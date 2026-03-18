using EmailAutomation.Web.Data;
using EmailAutomation.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=emailautomation.db"));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IRecipientService, RecipientService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IGraphMailService, GraphMailService>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    // Ensure EmailTemplates table exists (for DBs created before this table was added)
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS EmailTemplates (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Subject TEXT NOT NULL, Body TEXT NOT NULL, CreatedAt TEXT NOT NULL);");
    // Ensure Contacts table exists
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS Contacts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Email TEXT NOT NULL UNIQUE, Name TEXT NOT NULL, Mail1Date TEXT NULL, Mail2Date TEXT NULL, Mail3Date TEXT NULL, Mail4Date TEXT NULL, Mail5Date TEXT NULL, Ignore INTEGER NOT NULL, CreatedAt TEXT NOT NULL);");
    // Ensure Batches table exists
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS Batches (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CreatedAt TEXT NOT NULL);");
    // Ensure BatchContacts table exists
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS BatchContacts (BatchId INTEGER NOT NULL, ContactId INTEGER NOT NULL, PRIMARY KEY (BatchId, ContactId));");
    // Ensure ImportLogs table exists
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS ImportLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, FileName TEXT NOT NULL, ImportedAt TEXT NOT NULL, AddedCount INTEGER NOT NULL, SkippedCount INTEGER NOT NULL, ErrorsJson TEXT NULL);");
    // Recreate EmailJobs table so its foreign key points to Batches instead of legacy ImportBatches
    //
    // NOTE: This app uses EnsureCreated + a few idempotent DDL statements instead of EF migrations.
    // We keep schema changes additive (ALTER TABLE ADD COLUMN / CREATE TABLE IF NOT EXISTS) to avoid data loss.
    //
    static async Task<bool> ColumnExistsAsync(AppDbContext ctx, string tableName, string columnName)
    {
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1); // PRAGMA table_info: 1 = name
                if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    // Ensure EmailJobs has new columns (added over time).
    if (!await ColumnExistsAsync(db, "EmailJobs", "TemplateId"))
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE EmailJobs ADD COLUMN TemplateId INTEGER NULL;");
    }
    if (!await ColumnExistsAsync(db, "EmailJobs", "RetryOfJobId"))
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE EmailJobs ADD COLUMN RetryOfJobId INTEGER NULL;");
    }

    // Ensure EmailJobRecipients table exists for per-recipient tracking.
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS EmailJobRecipients (" +
        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
        "JobId INTEGER NOT NULL, " +
        "ContactId INTEGER NOT NULL, " +
        "Email TEXT NOT NULL, " +
        "Name TEXT NOT NULL, " +
        "Status TEXT NOT NULL, " +
        "ReasonCode TEXT NULL, " +
        "ReasonMessage TEXT NULL, " +
        "AttemptCount INTEGER NOT NULL DEFAULT 0, " +
        "FirstAttemptAt TEXT NULL, " +
        "LastAttemptAt TEXT NULL, " +
        "FOREIGN KEY (JobId) REFERENCES EmailJobs(Id) ON DELETE CASCADE, " +
        "FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE" +
        ");");
    await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_EmailJobRecipients_JobId_ContactId ON EmailJobRecipients (JobId, ContactId);");
    await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_EmailJobRecipients_JobId ON EmailJobRecipients (JobId);");
    await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_EmailJobRecipients_Status ON EmailJobRecipients (Status);");

    // Ensure ContactMailSteps table exists for unlimited follow-up steps (Mail1..MailN).
    await db.Database.ExecuteSqlRawAsync(
        "CREATE TABLE IF NOT EXISTS ContactMailSteps (" +
        "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
        "ContactId INTEGER NOT NULL, " +
        "StepNumber INTEGER NOT NULL, " +
        "SentAt TEXT NOT NULL, " +
        "FOREIGN KEY (ContactId) REFERENCES Contacts(Id) ON DELETE CASCADE" +
        ");");
    await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_ContactMailSteps_ContactId_StepNumber ON ContactMailSteps (ContactId, StepNumber);");
    await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_ContactMailSteps_ContactId ON ContactMailSteps (ContactId);");

    // Backfill historical Mail1..Mail5 columns into ContactMailSteps (idempotent).
    for (var step = 1; step <= 5; step++)
    {
        var col = $"Mail{step}Date";
        await db.Database.ExecuteSqlRawAsync(
            $"INSERT OR IGNORE INTO ContactMailSteps (ContactId, StepNumber, SentAt) " +
            $"SELECT Id, {step}, {col} FROM Contacts WHERE {col} IS NOT NULL;");
    }
}

app.UseCors();
app.UseStaticFiles();
app.UseDefaultFiles();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

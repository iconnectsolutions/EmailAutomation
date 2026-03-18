using EmailAutomation.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailAutomation.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<EmailJob> EmailJobs => Set<EmailJob>();
    public DbSet<EmailJobRecipient> EmailJobRecipients => Set<EmailJobRecipient>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactMailStep> ContactMailSteps => Set<ContactMailStep>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchContact> BatchContacts => Set<BatchContact>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255);
        });

        modelBuilder.Entity<Recipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasIndex(e => e.BatchId);
            entity.HasOne(e => e.Batch)
                .WithMany(b => b.Recipients)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateSubject).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.RetryOfJobId);
            entity.HasIndex(e => e.BatchId);
            entity.HasOne(e => e.Batch)
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<EmailTemplate>()
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<EmailJob>()
                .WithMany()
                .HasForeignKey(e => e.RetryOfJobId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmailJobRecipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.ReasonCode).HasMaxLength(100);
            entity.Property(e => e.ReasonMessage).HasMaxLength(2000);

            entity.HasIndex(e => new { e.JobId, e.ContactId }).IsUnique();
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Recipients)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.Body).HasMaxLength(10000);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<ContactMailStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepNumber);
            entity.Property(e => e.SentAt);

            entity.HasIndex(e => new { e.ContactId, e.StepNumber }).IsUnique();
            entity.HasIndex(e => e.ContactId);

            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<BatchContact>(entity =>
        {
            entity.HasKey(e => new { e.BatchId, e.ContactId });
            entity.HasOne(e => e.Batch)
                .WithMany(b => b.BatchContacts)
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Contact)
                .WithMany()
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(255);
        });
    }
}

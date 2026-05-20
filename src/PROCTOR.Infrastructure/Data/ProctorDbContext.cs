using Microsoft.EntityFrameworkCore;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data;

public class ProctorDbContext : DbContext
{
    public ProctorDbContext(DbContextOptions<ProctorDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<MenuPermission> MenuPermissions => Set<MenuPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Hearing> Hearings => Set<Hearing>();
    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<VerificationChecklistItem> VerificationChecklistItems => Set<VerificationChecklistItem>();
    public DbSet<CaseVerification> CaseVerifications => Set<CaseVerification>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Rank> Ranks => Set<Rank>();
    public DbSet<CaseComplainant> CaseComplainants => Set<CaseComplainant>();
    public DbSet<CaseAccused> CaseAccusedPersons => Set<CaseAccused>();
    public DbSet<ForwardingRule> ForwardingRules => Set<ForwardingRule>();
    public DbSet<CaseCategory> CaseCategories => Set<CaseCategory>();
    public DbSet<CaseAssignment> CaseAssignments => Set<CaseAssignment>();
    public DbSet<SentEmail> SentEmails => Set<SentEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProctorDbContext).Assembly);

        modelBuilder.Entity<CaseCategory>(b =>
        {
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<CaseAssignment>(b =>
        {
            b.HasOne(a => a.Case)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(a => new { a.CaseId, a.UserId, a.IsActive });
        });

        modelBuilder.Entity<Case>(b =>
        {
            b.HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SentEmail>(b =>
        {
            b.HasIndex(x => x.RelatedCaseId);
        });
    }
}

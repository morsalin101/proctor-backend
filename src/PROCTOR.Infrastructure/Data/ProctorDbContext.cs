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
    public DbSet<Student> Students => Set<Student>();
    public DbSet<CaseAdditionalInfo> CaseAdditionalInfos => Set<CaseAdditionalInfo>();
    public DbSet<Type3Workflow> Type3Workflows => Set<Type3Workflow>();
    public DbSet<Type3WorkflowTransition> Type3WorkflowTransitions => Set<Type3WorkflowTransition>();
    public DbSet<DcMemberRemark> DcMemberRemarks => Set<DcMemberRemark>();
    public DbSet<DisciplinaryResolution> DisciplinaryResolutions => Set<DisciplinaryResolution>();
    public DbSet<DisciplinaryResolutionCase> DisciplinaryResolutionCases => Set<DisciplinaryResolutionCase>();
    public DbSet<InvestigationAttachment> InvestigationAttachments => Set<InvestigationAttachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        modelBuilder.Entity<Type3Workflow>(b =>
        {
            b.Property(x => x.CurrentStage).HasConversion<string>();
            b.HasIndex(x => x.CaseId).IsUnique();
            b.HasOne(x => x.Case).WithOne(x => x.Type3Workflow)
                .HasForeignKey<Type3Workflow>(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.ReportId).IsUnique();
            b.HasOne(x => x.Report).WithMany()
                .HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Type3WorkflowTransition>(b =>
        {
            b.Property(x => x.FromStage).HasConversion<string>();
            b.Property(x => x.ToStage).HasConversion<string>();
            b.HasOne(x => x.Workflow).WithMany(x => x.Transitions)
                .HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.WorkflowId, x.FromStage, x.ToStage }).IsUnique();
        });

        modelBuilder.Entity<DcMemberRemark>(b =>
        {
            b.HasIndex(x => new { x.WorkflowId, x.MemberUserId }).IsUnique();
            b.HasOne(x => x.Workflow).WithMany(x => x.MemberRemarks)
                .HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DisciplinaryResolution>(b =>
        {
            b.Property(x => x.Status).HasConversion<string>();
            b.HasIndex(x => x.ResolutionNumber).IsUnique();
        });

        modelBuilder.Entity<DisciplinaryResolutionCase>(b =>
        {
            b.HasIndex(x => x.CaseId).IsUnique();
            b.HasOne(x => x.Resolution).WithMany(x => x.Cases)
                .HasForeignKey(x => x.ResolutionId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Case).WithMany()
                .HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvestigationAttachment>(b =>
        {
            b.HasOne(x => x.Case).WithMany(x => x.InvestigationAttachments)
                .HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => x.CaseId);
        });

        modelBuilder.Entity<SentEmail>(b =>
        {
            b.HasIndex(x => x.RelatedCaseId);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.Property(x => x.UserName).HasMaxLength(160).IsRequired();
            b.Property(x => x.UserRole).HasMaxLength(80).IsRequired();
            b.Property(x => x.Action).HasMaxLength(160).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            b.Property(x => x.EntityId).HasMaxLength(160);
            b.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
            b.Property(x => x.Path).HasMaxLength(500).IsRequired();
            b.Property(x => x.QueryString).HasMaxLength(4000);
            b.Property(x => x.IpAddress).HasMaxLength(64);
            b.Property(x => x.UserAgent).HasMaxLength(500);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Action);
            b.HasIndex(x => x.EntityType);
        });
    }
}

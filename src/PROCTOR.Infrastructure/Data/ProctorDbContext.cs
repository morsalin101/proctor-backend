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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProctorDbContext).Assembly);
    }
}

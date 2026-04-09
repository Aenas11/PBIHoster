using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Models;

namespace ReportTree.Server.Persistance.Relational;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<CustomTheme> Themes => Set<CustomTheme>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<AccountLockout> AccountLockouts => Set<AccountLockout>();
    public DbSet<DatasetRefreshSchedule> RefreshSchedules => Set<DatasetRefreshSchedule>();
    public DbSet<DatasetRefreshRun> RefreshRuns => Set<DatasetRefreshRun>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PageVersion> PageVersions => Set<PageVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
            entity.Property(user => user.Roles)
                .HasConversion(RelationalJsonConverters.StringListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringListComparer);
            entity.Property(user => user.FavoritePageIds)
                .HasConversion(RelationalJsonConverters.IntListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.IntListComparer);
            entity.Property(user => user.RecentPageIds)
                .HasConversion(RelationalJsonConverters.IntListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.IntListComparer);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(group => group.Name);
            entity.Property(group => group.Members)
                .HasConversion(RelationalJsonConverters.StringListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringListComparer);
        });

        modelBuilder.Entity<CustomTheme>(entity =>
        {
            entity.HasKey(theme => theme.Id);
            entity.HasIndex(theme => theme.OrganizationId);
            entity.HasIndex(theme => theme.CreatedBy);
            entity.Property(theme => theme.Tokens)
                .HasConversion(RelationalJsonConverters.StringDictionaryConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringDictionaryComparer);
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasIndex(page => page.ParentId);
            entity.HasIndex(page => page.IsPublic);
            entity.Property(page => page.AllowedUsers)
                .HasConversion(RelationalJsonConverters.StringListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringListComparer);
            entity.Property(page => page.AllowedGroups)
                .HasConversion(RelationalJsonConverters.StringListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringListComparer);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasIndex(setting => setting.Key).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(log => log.Username);
            entity.HasIndex(log => log.Resource);
            entity.HasIndex(log => log.Action);
            entity.HasIndex(log => log.Timestamp);
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(attempt => attempt.Username);
            entity.HasIndex(attempt => attempt.AttemptTime);
        });

        modelBuilder.Entity<AccountLockout>(entity =>
        {
            entity.HasIndex(lockout => lockout.Username).IsUnique();
        });

        modelBuilder.Entity<DatasetRefreshSchedule>(entity =>
        {
            entity.HasKey(schedule => schedule.Id);
            entity.HasIndex(schedule => schedule.WorkspaceId);
            entity.HasIndex(schedule => schedule.DatasetId);
            entity.HasIndex(schedule => schedule.Enabled);
            entity.Property(schedule => schedule.NotifyTargets)
                .HasConversion(RelationalJsonConverters.NotificationTargetsConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.NotificationTargetsComparer);
        });

        modelBuilder.Entity<DatasetRefreshRun>(entity =>
        {
            entity.HasKey(run => run.Id);
            entity.HasIndex(run => run.DatasetId);
            entity.HasIndex(run => run.ScheduleId);
            entity.HasIndex(run => run.Status);
            entity.HasIndex(run => run.RequestedAtUtc);
            entity.Property(run => run.Status).HasConversion<string>();
        });

        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.HasIndex(evt => evt.Timestamp);
            entity.HasIndex(evt => evt.EventType);
            entity.HasIndex(evt => evt.Username);
            entity.HasIndex(evt => evt.Path);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasIndex(comment => comment.PageId);
            entity.HasIndex(comment => comment.ParentId);
            entity.HasIndex(comment => comment.Username);
            entity.HasIndex(comment => comment.CreatedAt);
            entity.Property(comment => comment.Mentions)
                .HasConversion(RelationalJsonConverters.StringListConverter)
                .Metadata.SetValueComparer(RelationalJsonConverters.StringListComparer);
        });

        modelBuilder.Entity<PageVersion>(entity =>
        {
            entity.HasIndex(version => version.PageId);
            entity.HasIndex(version => version.ChangedAt);
        });
    }
}
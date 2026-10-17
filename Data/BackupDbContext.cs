using Backuper.Models;
using Microsoft.EntityFrameworkCore;

namespace Backuper.Data;

public sealed class BackupDbContext(DbContextOptions<BackupDbContext> options) : DbContext(options)
{
    public DbSet<BackupExecution> BackupExecutions => Set<BackupExecution>();

    public DbSet<BackupState> BackupStates => Set<BackupState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BackupExecution>(entity =>
        {
            entity.HasKey(execution => execution.Id);
            entity.Property(execution => execution.ScheduledDate).HasMaxLength(10).IsRequired();
            entity.Property(execution => execution.Status).HasMaxLength(32).IsRequired();
            entity.Property(execution => execution.LogFilePath).HasMaxLength(512).IsRequired();
            entity.Property(execution => execution.ErrorMessage).HasMaxLength(2048);
            entity.HasIndex(execution => new { execution.ScheduledDate, execution.Status });
        });

        modelBuilder.Entity<BackupState>(entity =>
        {
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).ValueGeneratedNever();
            entity.Property(state => state.LastBackupStatus).HasMaxLength(32).IsRequired();
        });
    }

    public async Task<BackupState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var state = await BackupStates.SingleOrDefaultAsync(state => state.Id == BackupState.SingletonId, cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = new BackupState { Id = BackupState.SingletonId };
        BackupStates.Add(state);
        await SaveChangesAsync(cancellationToken);
        return state;
    }

    public async Task MarkInterruptedRunsAsInterruptedAsync()
    {
        var interruptedRuns = await BackupExecutions
            .Where(execution => execution.Status == BackupStatus.InProgress)
            .ToListAsync();

        if (interruptedRuns.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        foreach (var run in interruptedRuns)
        {
            run.Status = BackupStatus.Interrupted;
            run.CompletedAtLocal = now;
            run.CompletedAtUtc = now.ToUniversalTime();
            run.ErrorMessage = "The previous service process stopped before the backup completed.";
        }

        var state = await GetOrCreateStateAsync(CancellationToken.None);
        state.LastBackupStatus = BackupStatus.Interrupted;
        state.LastBackupCompletedAtLocal = now;
        state.LastBackupCompletedAtUtc = now.ToUniversalTime();

        await SaveChangesAsync();
    }
}

using Backuper.Models;
using Microsoft.EntityFrameworkCore;

namespace Backuper.Data;

/// <summary>Persists backup attempts and aggregate state in the configured SQLite database.</summary>
public sealed class BackupDbContext(DbContextOptions<BackupDbContext> options) : DbContext(options)
{
    /// <summary>Backup attempt history used for scheduling and diagnostics.</summary>
    public DbSet<BackupExecution> BackupExecutions => Set<BackupExecution>();

    /// <summary>Singleton backup-state summary.</summary>
    public DbSet<BackupState> BackupStates => Set<BackupState>();

    /// <inheritdoc/>
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

    /// <summary>Loads the singleton state row, creating and saving it if missing.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The tracked aggregate state entity.</returns>
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

    /// <summary>Marks all unfinished attempts as interrupted when the host starts.</summary>
    /// <remarks>An InProgress row left by a previous process is treated as interrupted; this does not resume rclone.</remarks>
    /// <returns>A task that completes after recovery changes have been saved.</returns>
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

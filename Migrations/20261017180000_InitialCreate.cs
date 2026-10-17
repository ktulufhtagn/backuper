using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backuper.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BackupExecutions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ScheduledDate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                StartedAtLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CompletedAtLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                LogFilePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BackupExecutions", execution => execution.Id);
            });

        migrationBuilder.CreateTable(
            name: "BackupStates",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                LastBackupStartedAtLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastBackupStartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastBackupCompletedAtLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastBackupCompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastSuccessfulBackupAtLocal = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastSuccessfulBackupAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                LastBackupStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BackupStates", state => state.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BackupExecutions_ScheduledDate_Status",
            table: "BackupExecutions",
            columns: new[] { "ScheduledDate", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BackupExecutions");
        migrationBuilder.DropTable(name: "BackupStates");
    }
}

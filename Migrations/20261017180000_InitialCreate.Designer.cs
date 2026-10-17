using System;
using Backuper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backuper.Migrations;

[DbContext(typeof(BackupDbContext))]
[Migration("20261017180000_InitialCreate")]
public partial class InitialCreate
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");

        modelBuilder.Entity("Backuper.Models.BackupExecution", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<DateTimeOffset?>("CompletedAtLocal")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("CompletedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("ErrorMessage")
                .HasMaxLength(2048)
                .HasColumnType("TEXT");

            entity.Property<int?>("ExitCode")
                .HasColumnType("INTEGER");

            entity.Property<string>("LogFilePath")
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("TEXT");

            entity.Property<string>("ScheduledDate")
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("StartedAtLocal")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("StartedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("ScheduledDate", "Status");

            entity.ToTable("BackupExecutions");
        });

        modelBuilder.Entity("Backuper.Models.BackupState", entity =>
        {
            entity.Property<int>("Id")
                .HasColumnType("INTEGER");

            entity.Property<DateTimeOffset?>("LastBackupCompletedAtLocal")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("LastBackupCompletedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("LastBackupStartedAtLocal")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("LastBackupStartedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("LastBackupStatus")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("LastSuccessfulBackupAtLocal")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset?>("LastSuccessfulBackupAtUtc")
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.ToTable("BackupStates");
        });
#pragma warning restore 612, 618
    }
}

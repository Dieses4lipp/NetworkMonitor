using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;

namespace NetworkMonitor.Infrastructure.Data.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<MonitoringJob> MonitoringJobs { get; set; }

    public virtual DbSet<RawMetric> RawMetrics { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>()
    .HasMany(a => a.Devices)
    .WithOne(d => d.Agent)
    .HasForeignKey(d => d.AgentId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Devices_pkey");

            entity.HasOne(d => d.Agent).WithMany(p => p.Devices)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_Devices_Agents");
        });

        modelBuilder.Entity<MonitoringJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MonitoringJobs_pkey");

            entity.Property(e => e.ConfigurationJson).HasColumnType("jsonb");
            entity.Property(e => e.IntervalSeconds).HasDefaultValue(60);

            entity.HasOne(d => d.Device).WithMany(p => p.MonitoringJobs)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_Jobs_Devices");
        });

        modelBuilder.Entity<RawMetric>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RawMetrics_pkey");

            entity.HasIndex(e => e.JobId, "IX_Devices_AgentId");

            entity.HasIndex(e => new { e.JobId, e.Timestamp }, "IX_RawMetrics_JobId_Timestamp").IsDescending(false, true);

            entity.Property(e => e.Timestamp).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Job).WithMany(p => p.RawMetrics)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("FK_Metrics_Jobs");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

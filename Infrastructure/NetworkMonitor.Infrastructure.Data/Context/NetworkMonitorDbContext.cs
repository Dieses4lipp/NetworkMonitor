using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;

namespace NetworkMonitor.Infrastructure.Data.Context;

public class NetworkMonitorDbContext : DbContext
{
    public NetworkMonitorDbContext()
    {
    }

    public NetworkMonitorDbContext(DbContextOptions<NetworkMonitorDbContext> options)
        : base(options)
    {
    }

    // Monitoring entities
    public virtual DbSet<Agent> Agents { get; set; }
    public virtual DbSet<Device> Devices { get; set; }
    public virtual DbSet<MonitoringJob> MonitoringJobs { get; set; }
    public virtual DbSet<RawMetric> RawMetrics { get; set; }
    public virtual DbSet<NetworkScan> NetworkScans { get; set; }
    public virtual DbSet<DeviceHistory> DeviceHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Agent relationships
        modelBuilder.Entity<Agent>()
            .HasMany(a => a.Devices)
            .WithOne(d => d.Agent)
            .HasForeignKey(d => d.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Device relationships
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Devices_pkey");

            entity.HasOne(d => d.Agent).WithMany(p => p.Devices)
                .HasForeignKey(d => d.AgentId)
                .HasConstraintName("FK_Devices_Agents");
        });

        // Configure MonitoringJob relationships
        modelBuilder.Entity<MonitoringJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MonitoringJobs_pkey");

            entity.Property(e => e.ConfigurationJson).HasColumnType("jsonb");
            entity.Property(e => e.IntervalSeconds).HasDefaultValue(60);

            entity.HasOne(d => d.Device).WithMany(p => p.MonitoringJobs)
                .HasForeignKey(d => d.DeviceId)
                .HasConstraintName("FK_Jobs_Devices");
        });

        // Configure RawMetric relationships
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

        // Configure NetworkScan
        modelBuilder.Entity<NetworkScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartTime).IsRequired();
            entity.HasIndex(e => e.StartTime);
        });

        // Configure DeviceHistory
        modelBuilder.Entity<DeviceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.ScanId);
        });

        base.OnModelCreating(modelBuilder);
    }
}

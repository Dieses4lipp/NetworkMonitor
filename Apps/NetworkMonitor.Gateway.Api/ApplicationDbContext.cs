using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Data;

namespace NetworkMonitor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NetworkDevice> Devices { get; set; }
        public DbSet<NetworkScan> Scans { get; set; }
        public DbSet<DeviceHistory> DeviceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NetworkDevice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IPAddress).IsRequired().HasMaxLength(15);
                entity.Property(e => e.MACAddress).IsRequired().HasMaxLength(17);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.HasIndex(e => e.MACAddress).IsUnique();
            });

            modelBuilder.Entity<NetworkScan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartTime).IsRequired();
                entity.HasIndex(e => e.StartTime);
            });

            modelBuilder.Entity<DeviceHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.HasIndex(e => e.DeviceId);
                entity.HasIndex(e => e.ScanId);
            });
        }
    }
}

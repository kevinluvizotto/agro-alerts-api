using Microsoft.EntityFrameworkCore;

namespace AgroAlerts.Api.Data;

public class AlertsDbContext(DbContextOptions<AlertsDbContext> options) : DbContext(options)
{
    public DbSet<IncomingReading> IncomingReadings => Set<IncomingReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("alerts");

        modelBuilder.Entity<IncomingReading>(e =>
        {
            e.ToTable("IncomingReadings");
            e.HasKey(x => x.Id);
            e.Property(x => x.SoilMoisture).HasPrecision(18, 2);
            e.Property(x => x.TemperatureC).HasPrecision(18, 2);
            e.Property(x => x.PrecipitationMm).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.ToTable("Alerts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            e.Property(x => x.Message).HasMaxLength(300).IsRequired();
            e.Property(x => x.SoilMoisture).HasPrecision(18, 2);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class IncomingReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal PrecipitationMm { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Type { get; set; } = default!;
    public string Severity { get; set; } = "WARN";
    public string Message { get; set; } = default!;
    public decimal SoilMoisture { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Acknowledged { get; set; }
}
namespace AgroAlerts.Api.Messaging;

public record SensorReadingReceived(
    Guid PlotId,
    DateTimeOffset Timestamp,
    decimal SoilMoisture,
    decimal TemperatureC,
    decimal PrecipitationMm
);
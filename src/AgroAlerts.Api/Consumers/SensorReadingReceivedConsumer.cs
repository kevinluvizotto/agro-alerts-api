using AgroAlerts.Api.Data;
using AgroAlerts.Api.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AgroAlerts.Api.Consumers;

public class SensorReadingReceivedConsumer(AlertsDbContext db, ILogger<SensorReadingReceivedConsumer> logger)
    : IConsumer<SensorReadingReceived>
{
    private const decimal MoistureThreshold = 30m;
    private static readonly TimeSpan DroughtWindow = TimeSpan.FromHours(24);
    private const string AlertType = "LOW_MOISTURE"; // este tipo representa "Alerta de Seca"

    public async Task Consume(ConsumeContext<SensorReadingReceived> context)
    {
        var msg = context.Message;

        logger.LogInformation("Received reading PlotId={PlotId} Moisture={Moisture} Timestamp={Timestamp}",
            msg.PlotId, msg.SoilMoisture, msg.Timestamp);

        // 1) Sempre salva a leitura recebida
        db.IncomingReadings.Add(new IncomingReading
        {
            Id = Guid.NewGuid(),
            PlotId = msg.PlotId,
            Timestamp = msg.Timestamp,
            SoilMoisture = msg.SoilMoisture,
            TemperatureC = msg.TemperatureC,
            PrecipitationMm = msg.PrecipitationMm,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        // 2) Só avalia regra de seca se a leitura atual estiver abaixo do limiar
        var shouldEvaluate = msg.SoilMoisture < MoistureThreshold;

        if (shouldEvaluate)
        {
            // Não duplicar alertas: se já existe um alerta pendente para o talhão, não cria outro
            var hasOpenAlert = await db.Alerts.AnyAsync(a =>
                a.PlotId == msg.PlotId &&
                a.Type == AlertType &&
                a.Acknowledged == false);

            if (!hasOpenAlert)
            {
                // lastOk = última leitura (no histórico) com umidade >= 30
                var lastOk = await db.IncomingReadings
                    .Where(r => r.PlotId == msg.PlotId && r.SoilMoisture >= MoistureThreshold)
                    .MaxAsync(r => (DateTimeOffset?)r.Timestamp);

                // dryStart = primeira leitura < 30 após o lastOk (início da "sequência de seca")
                DateTimeOffset? dryStartDb;

                if (lastOk.HasValue)
                {
                    dryStartDb = await db.IncomingReadings
                        .Where(r => r.PlotId == msg.PlotId &&
                                    r.SoilMoisture < MoistureThreshold &&
                                    r.Timestamp > lastOk.Value)
                        .MinAsync(r => (DateTimeOffset?)r.Timestamp);
                }
                else
                {
                    dryStartDb = await db.IncomingReadings
                        .Where(r => r.PlotId == msg.PlotId &&
                                    r.SoilMoisture < MoistureThreshold)
                        .MinAsync(r => (DateTimeOffset?)r.Timestamp);
                }

                var dryStart = dryStartDb ?? msg.Timestamp;

                // Se a mensagem vier fora de ordem, evita duração negativa
                if (msg.Timestamp < dryStart) dryStart = msg.Timestamp;

                var duration = msg.Timestamp - dryStart;
                if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

                // 3) Regra principal: <30 por >= 24h => cria "Alerta de Seca"
                if (duration >= DroughtWindow)
                {
                    db.Alerts.Add(new Alert
                    {
                        Id = Guid.NewGuid(),
                        PlotId = msg.PlotId,
                        Timestamp = msg.Timestamp,
                        Type = AlertType,
                        Severity = msg.SoilMoisture < 15 ? "CRITICAL" : "WARN",
                        SoilMoisture = msg.SoilMoisture,
                        Message = $"Alerta de Seca: umidade < 30% por {duration.TotalHours:F1}h (desde {dryStart:O}). Umidade atual: {msg.SoilMoisture}",
                        CreatedAt = DateTimeOffset.UtcNow,
                        Acknowledged = false
                    });

                    logger.LogWarning("DROUGHT ALERT created PlotId={PlotId} DurationHours={Hours} Moisture={Moisture}",
                        msg.PlotId, duration.TotalHours, msg.SoilMoisture);
                }
                else
                {
                    logger.LogInformation("Dry streak not long enough yet PlotId={PlotId} DurationHours={Hours}",
                        msg.PlotId, duration.TotalHours);
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
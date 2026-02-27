using AgroAlerts.Api.Messaging;
using MassTransit;

namespace AgroAlerts.Api.Consumers;

public class SensorReadingValidatedConsumer : IConsumer<SensorReadingReceived>
{
    public Task Consume(ConsumeContext<SensorReadingReceived> context)
    {
        var msg = context.Message;

        Console.WriteLine($"[VALIDATED] PlotId={msg.PlotId} Moisture={msg.SoilMoisture} Timestamp={msg.Timestamp}");

        return Task.CompletedTask;
    }
}
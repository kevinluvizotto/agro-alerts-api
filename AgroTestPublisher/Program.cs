using MassTransit;
using AgroAlerts.Api.Messaging;

var rabbitHost = "localhost";
var rabbitUser = "agro";
var rabbitPass = "agro";

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host(rabbitHost, "/", h =>
    {
        h.Username(rabbitUser);
        h.Password(rabbitPass);
    });
});

await bus.StartAsync();
try
{
    var msg = new SensorReadingReceived(
        PlotId: Guid.NewGuid(),
        Timestamp: DateTimeOffset.UtcNow,
        SoilMoisture: 42.5M,
        TemperatureC: 28.3M,
        PrecipitationMm: 1.2M
    );

    Console.WriteLine("Publishing SensorReadingReceived...");
    await bus.Publish(msg);
    Console.WriteLine("Published!");
}
finally
{
    await bus.StopAsync();
}
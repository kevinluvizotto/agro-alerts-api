using AgroAlerts.Api.Consumers;
using AgroAlerts.Api.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["Rabbit:User"] ?? "agro";
var rabbitPass = builder.Configuration["Rabbit:Pass"] ?? "agro";

// DB (schema alerts)
builder.Services.AddDbContext<AlertsDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "alerts")
                  .EnableRetryOnFailure()
    )
);

// MassTransit consumer
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<SensorReadingReceivedConsumer>();
    x.AddConsumer<SensorReadingValidatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroAlerts API", Version = "v1" });
});

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/alerts", async (AlertsDbContext db, string? severity, bool? ack, Guid? plotId) =>
{
    var q = db.Alerts.AsQueryable();

    if (!string.IsNullOrWhiteSpace(severity))
        q = q.Where(a => a.Severity == severity);

    if (ack.HasValue)
        q = q.Where(a => a.Acknowledged == ack.Value);

    if (plotId.HasValue)
        q = q.Where(a => a.PlotId == plotId.Value);

    var items = await q
        .OrderByDescending(a => a.CreatedAt)
        .Take(50)
        .Select(a => new
        {
            a.Id,
            a.PlotId,
            a.Type,
            a.Severity,
            a.SoilMoisture,
            a.Timestamp,
            a.CreatedAt,
            a.Acknowledged,
            a.Message
        })
        .ToListAsync();

    return Results.Ok(items);
});

app.MapPut("/alerts/{id:guid}/ack", async (Guid id, AlertsDbContext db) =>
{
    var alert = await db.Alerts.FindAsync(id);
    if (alert is null) return Results.NotFound();

    alert.Acknowledged = true;
    await db.SaveChangesAsync();

    return Results.Ok(new { alert.Id, alert.Acknowledged });
});

app.Run();
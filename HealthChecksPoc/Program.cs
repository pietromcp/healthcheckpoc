using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var randomHealthCheckConfiguration = new RandomHealthCheckConfiguration(
    int.Parse(Environment.GetEnvironmentVariable("Range") ?? "100"),
    int.Parse(Environment.GetEnvironmentVariable("UnhealthyThreshold") ?? "100"),
    int.Parse(Environment.GetEnvironmentVariable("DegradedThreshold") ?? "100")
);

builder.Services.AddSingleton(randomHealthCheckConfiguration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddMongoDb("mongodb://mongo")
    .AddRabbitMQ(p => new ConnectionFactory {
        Uri = new Uri("amqp://guest:guest@rabbitmq/atlantis")
    })
    .AddEventStore("ConnectTo=tcp://admin:changeit@eventstore:1113;")
    .AddCheck<RandomHealthCheck>("Random")
    .AddCheck<MinuteModulusHealthCheck>("MinuteModulus")
    ;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseRouting();
app.UseEndpoints(config =>
{
    config.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
});

app.Run();

public record RandomHealthCheckConfiguration(int Range, int UnhealthyThreshold, int DegradedThreshold);

public class RandomHealthCheck : IHealthCheck {
    private readonly RandomHealthCheckConfiguration config;
    private readonly Random randomizer = new Random();

    public RandomHealthCheck(RandomHealthCheckConfiguration config) {
        this.config = config;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken()) {
        var number = randomizer.Next(config.Range);
        if (number > config.UnhealthyThreshold)
            return Task.FromResult(HealthCheckResult.Unhealthy());
        if (number> config.DegradedThreshold)
            return Task.FromResult(HealthCheckResult.Degraded());
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

public class MinuteModulusHealthCheck : IHealthCheck {
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken()) {
        var minute = DateTimeOffset.Now.Minute;
        var modulus = minute % 6;
        if (modulus == 0)
            return Task.FromResult(HealthCheckResult.Unhealthy());
        if (modulus <= 2)
            return Task.FromResult(HealthCheckResult.Degraded());
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

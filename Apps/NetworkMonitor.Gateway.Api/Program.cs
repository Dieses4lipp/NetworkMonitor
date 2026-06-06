using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=192.168.178.172;Database=NetworkMonitor;Username=postgres;Password=postgres";
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<INetworkDiscoveryService, NetworkDiscoveryService>();
builder.Services.AddHostedService<PeriodicNetworkScanWorker>();
builder.Services.AddHttpClient("MonitorClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

builder.Services.AddHostedService<JobExecutionWorker>();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NetworkMonitorDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

    if (!dbContext.Agents.Any(a => a.Id == SystemConstants.BuiltInAgentId))
    {
        dbContext.Agents.Add(new Agent
        {
            Id = SystemConstants.BuiltInAgentId,
            Name = "Gateway-Local-Scanner",
            SecretKey = "local-internal-no-auth-needed",
            LastSeen = DateTime.UtcNow,
            Version = "1.0.0"
        });
        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

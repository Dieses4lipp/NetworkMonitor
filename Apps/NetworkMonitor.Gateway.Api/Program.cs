using Microsoft.EntityFrameworkCore;
using NetworkMonitor;
using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api;
using NetworkMonitor.Infrastructure.Data.Context;
using NetworkMonitor.Services;
using System.Net;
using System.Net.Security;

var connectionString = "Host=192.168.178.172;Database=NetworkMonitor;Username=postgres;Password=postgres";
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<INetworkDiscoveryService, NetworkDiscoveryService>();
builder.Services.AddHostedService<PeriodicNetworkScanWorker>();
// Add this line to support the HTTP checks in our worker
builder.Services.AddHttpClient("MonitorClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Optional: Ignore SSL errors for homelab self-signed certs (like Proxmox)
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Register the new worker
builder.Services.AddHostedService<JobExecutionWorker>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();

ServicePointManager.ServerCertificateValidationCallback =
    delegate (object sender,
              System.Security.Cryptography.X509Certificates.X509Certificate certificate,
              System.Security.Cryptography.X509Certificates.X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
    {
        return true;
    };


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

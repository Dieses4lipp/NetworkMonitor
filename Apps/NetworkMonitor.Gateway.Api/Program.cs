using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api;
using NetworkMonitor.Infrastructure.Data.Context;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("NETWORKMONITOR_DB_CONNECTION")
    ?? throw new InvalidOperationException(
        "No database connection string configured. Set ConnectionStrings:DefaultConnection or the NETWORKMONITOR_DB_CONNECTION environment variable.");

var apiKey = builder.Configuration["ApiKey"]
    ?? Environment.GetEnvironmentVariable("NETWORKMONITOR_API_KEY")
    ?? throw new InvalidOperationException(
        "No API key configured. Set ApiKey or the NETWORKMONITOR_API_KEY environment variable.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<INetworkDiscoveryService, NetworkDiscoveryService>();
builder.Services.AddScoped<INetworkScanService, NetworkScanService>();
builder.Services.AddHostedService<PeriodicNetworkScanWorker>();
var allowInsecureTls = builder.Configuration.GetValue<bool>("NetworkMonitor:AllowSelfSignedCertificates");
builder.Services.AddHttpClient("MonitorClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = allowInsecureTls
            ? (message, cert, chain, errors) => true
            : null
    });
builder.Services.AddSingleton<IVendorLookupService, VendorLookupService>();
builder.Services.AddHostedService<JobExecutionWorker>();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NetworkMonitorDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();
var vendorLookup = app.Services.GetRequiredService<IVendorLookupService>();
await ((VendorLookupService)vendorLookup).InitializeAsync();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

    if (!dbContext.Agents.Any(a => a.Id == SystemConstants.BuiltInAgentId))
    {
        dbContext.Agents.Add(new Agent
        {
            Id = SystemConstants.BuiltInAgentId,
            Name = "Gateway-Local-Scanner",
            SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            LastSeen = DateTime.UtcNow,
            Version = "1.0.0"
        });
        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyMiddleware>(apiKey);
app.UseAuthorization();
app.MapControllers();

app.Run();

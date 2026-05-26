using System.Net;
using System.Net.Security;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor;
using NetworkMonitor.Infrastructure.Data.Context;
using RS.Fritz.Manager.API;
using NetworkMonitor.Services;
using NetworkMonitor.Data; // Add using for ApplicationDbContext

var connectionString = "Host=192.168.178.172;Database=NetworkMonitor;Username=postgres;Password=postgres";
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFritzApi();

builder.Services.AddScoped<IDeviceTrackingService, DeviceTrackingService>();
builder.Services.AddScoped<INetworkDiscoveryService, NetworkDiscoveryService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();


ServicePointManager.ServerCertificateValidationCallback =
    delegate (object sender,
              System.Security.Cryptography.X509Certificates.X509Certificate certificate,
              System.Security.Cryptography.X509Certificates.X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
    {
        return true;
    };


// Register MyDbContext if it's still needed, otherwise this can be removed.
builder.Services.AddDbContext<MyDbContext>(options => options.UseNpgsql(connectionString));

// Register ApplicationDbContext to fix the injection error
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

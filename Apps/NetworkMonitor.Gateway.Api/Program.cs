using System.Net;
using System.Net.Security;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor;
using NetworkMonitor.Infrastructure.Data.Context;
using RS.Fritz.Manager.API;
using NetworkMonitor.Services;

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


builder.Services.AddDbContext<NetworkMonitorDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

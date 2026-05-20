using System.Net;
using System.Net.Security;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor;
using NetworkMonitor.Infrastructure.Data.Context;
using RS.Fritz.Manager.API;

var connectionString = "Host=192.168.178.116;Database=NetworkMonitor;Username=postgres;Password=Database123!";
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFritzApi();
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


ServicePointManager.ServerCertificateValidationCallback =
    delegate (object sender,
              System.Security.Cryptography.X509Certificates.X509Certificate certificate,
              System.Security.Cryptography.X509Certificates.X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
    {
        return true;
    };


builder.Services.AddDbContext<MyDbContext>(options => options.UseNpgsql(connectionString));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

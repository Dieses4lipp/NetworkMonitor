using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NetworkMonitor.Domain;
using NetworkMonitor.Gateway.Api.PlatformInspection;
using NetworkMonitor.Gateway.Api.ServiceDiscovery;
using Xunit;

namespace NetworkMonitor.Gateway.Api.Tests;

/// <summary>
/// Drives <see cref="NetworkScanService.RunScanAsync"/> against a real PostgreSQL database and
/// asserts that a rescan of the same device converges on the ports that are currently open.
/// </summary>
public sealed class NetworkServiceReconcileTests
{
    private const string DeviceIp = "10.99.0.5";
    private const int SshPort = 22;
    private const int HttpPort = 80;
    private const int HttpsPort = 443;

    /// <summary>The interval shipped in appsettings.json.</summary>
    private const int ProductionFingerprintIntervalMinutes = 30;

    private readonly PostgresTestDatabase _database = new();
    private readonly StubNetworkDiscoveryService _discovery = new();
    private readonly StubServiceDiscoveryService _serviceDiscovery = new();

    public NetworkServiceReconcileTests()
    {
        _discovery.Devices.Add(new DiscoveredDevice
        {
            IPAddress = DeviceIp,
            MACAddress = "00:11:22:33:44:55",
            HostName = "test-host",
            Vendor = "Test Vendor",
            OperatingSystem = "Linux",
            DiscoveredAt = DateTime.UtcNow
        });

        using var context = _database.CreateContext();
        context.Agents.Add(new Agent
        {
            Id = SystemConstants.BuiltInAgentId,
            Name = "Built-in",
            SecretKey = "test"
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task FirstScanOfADevicePersistsItsOpenPorts()
    {
        _serviceDiscovery.Services = OpenPorts(SshPort, HttpPort);

        await RunScanAsync();

        var stored = await LoadServicesAsync();
        Assert.Equal(new[] { SshPort, HttpPort }, stored.Select(s => s.Port).Order());
    }

    [Fact]
    public async Task RescanKeepsFirstSeenAtOnAPortThatIsStillOpen()
    {
        _serviceDiscovery.Services = OpenPorts(SshPort, HttpPort);
        await RunScanAsync();
        var firstSeenAt = (await LoadServicesAsync()).Single(s => s.Port == SshPort).FirstSeenAt;

        _serviceDiscovery.Services = OpenPorts(SshPort, HttpsPort);
        await RunScanAsync();

        var ssh = (await LoadServicesAsync()).Single(s => s.Port == SshPort);
        Assert.Equal(firstSeenAt, ssh.FirstSeenAt);
        Assert.True(ssh.LastSeenAt >= firstSeenAt);
    }

    [Fact]
    public async Task RescanAddsARowForANewlyOpenedPort()
    {
        _serviceDiscovery.Services = OpenPorts(SshPort, HttpPort);
        await RunScanAsync();

        _serviceDiscovery.Services = OpenPorts(SshPort, HttpsPort);
        await RunScanAsync();

        var stored = await LoadServicesAsync();
        Assert.Contains(stored, s => s.Port == HttpsPort);
    }

    [Fact]
    public async Task RescanRemovesTheRowForAPortThatClosed()
    {
        _serviceDiscovery.Services = OpenPorts(SshPort, HttpPort);
        await RunScanAsync();

        _serviceDiscovery.Services = OpenPorts(SshPort, HttpsPort);
        await RunScanAsync();

        var stored = await LoadServicesAsync();
        Assert.DoesNotContain(stored, s => s.Port == HttpPort);
        Assert.Equal(new[] { SshPort, HttpsPort }, stored.Select(s => s.Port).Order());
    }

    [Fact]
    public async Task FirstScanPersistsOpenPortsUnderTheConfiguredFingerprintInterval()
    {
        _serviceDiscovery.Services = OpenPorts(SshPort, HttpPort);

        await RunScanAsync(ProductionFingerprintIntervalMinutes);

        Assert.Equal(1, _serviceDiscovery.ScanCount);
        var stored = await LoadServicesAsync();
        Assert.Equal(new[] { SshPort, HttpPort }, stored.Select(s => s.Port).Order());
    }

    private static List<DiscoveredService> OpenPorts(params int[] ports)
        => ports.Select(p => new DiscoveredService(p, $"service-{p}", $"banner-{p}")).ToList();

    private async Task<List<NetworkService>> LoadServicesAsync()
    {
        await using var context = _database.CreateContext();
        return await context.NetworkServices.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Fingerprinting is throttled by an interval, so the reconcile tests set it to zero to make
    /// every scan reconcile. A scan gets its own DbContext, the way the hosted worker scopes one
    /// per run.
    /// </summary>
    private async Task RunScanAsync(int fingerprintIntervalMinutes = 0)
    {
        await using var context = _database.CreateContext();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NetworkMonitor:FingerprintIntervalMinutes"] =
                    fingerprintIntervalMinutes.ToString()
            })
            .Build();

        var scanService = new NetworkScanService(
            NullLogger<NetworkScanService>.Instance,
            _discovery,
            new PlatformClassificationService(),
            _serviceDiscovery,
            Array.Empty<IPlatformInspector>(),
            context,
            configuration);

        await scanService.RunScanAsync(CancellationToken.None);
    }
}

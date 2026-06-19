using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using NetworkMonitor.Gateway.Api;

namespace NetworkMonitor.Services
{
    public interface INetworkDiscoveryService
    {
        Task<List<DiscoveredDevice>> ScanNetworkAsync(CancellationToken cancellationToken = default);
        Task<List<DiscoveredDevice>> PingRangeAsync(string gatewayPrefix, int startIp = 1, int endIp = 254, CancellationToken cancellationToken = default);
    }

    public class DiscoveredDevice
    {
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public string HostName { get; set; }
        public string? Vendor { get; set; }
        public string InterfaceType { get; set; } = "Unknown";
        public string OperatingSystem { get; set; } = "Unknown";
        public DateTime DiscoveredAt { get; set; }
    }

    public class NetworkDiscoveryService : INetworkDiscoveryService
    {
        private readonly ILogger<NetworkDiscoveryService> _logger;
        private readonly IVendorLookupService _vendorLookup;
        public NetworkDiscoveryService(
            ILogger<NetworkDiscoveryService> logger,
            IVendorLookupService vendorLookup)
        {
            _logger = logger;
            _vendorLookup = vendorLookup;
        }

        public async Task<List<DiscoveredDevice>> ScanNetworkAsync(CancellationToken cancellationToken = default)
        {
            var devices = new List<DiscoveredDevice>();

            try
            {
                var gateway = GetGatewayAddress();
                if (gateway == null)
                {
                    _logger.LogWarning("No IPv4 gateway found");
                    return devices;
                }

                var gatewayPrefix = GetGatewayPrefix(gateway.ToString());
                _logger.LogInformation($"Starting network scan on {gatewayPrefix}*");

                devices = await PingRangeAsync(gatewayPrefix, cancellationToken: cancellationToken);

                _logger.LogInformation($"Network scan completed. Found {devices.Count} active devices");
                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during network scan");
                return devices;
            }
        }

        public async Task<List<DiscoveredDevice>> PingRangeAsync(string gatewayPrefix, int startIp = 1, int endIp = 254, CancellationToken cancellationToken = default)
        {
            var devices = new List<DiscoveredDevice>();
            var pingTasks = new List<Task<DiscoveredDevice>>();

            for (int i = startIp; i <= endIp; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                string ipAddress = $"{gatewayPrefix}{i}";
                pingTasks.Add(PingAddressAsync(ipAddress, cancellationToken));

                if (pingTasks.Count >= 10)
                {
                    var results = await Task.WhenAll(pingTasks);
                    devices.AddRange(results.Where(r => r != null));
                    pingTasks.Clear();
                }
            }

            if (pingTasks.Count > 0)
            {
                var results = await Task.WhenAll(pingTasks);
                devices.AddRange(results.Where(r => r != null));
            }

            return devices;
        }

        private async Task<DiscoveredDevice> PingAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                PingReply? reply = null;
                using (var ping = new Ping())
                {
                    reply = await ping.SendPingAsync(ipAddress, 1500);
                }

                var icmpAlive = reply?.Status == IPStatus.Success;
                var mac = GetMacAddressForIp(ipAddress);
                var isAlive = icmpAlive || mac != "Unknown";

                if (!isAlive) return null;

                var hostname = await GetHostNameAsync(ipAddress);
                var os = GuessOperatingSystem(reply?.Options?.Ttl);
                var vendor = _vendorLookup.Lookup(mac);

                return new DiscoveredDevice
                {
                    IPAddress = ipAddress,
                    MACAddress = mac,
                    Vendor = vendor,
                    HostName = hostname,
                    InterfaceType = DetermineInterfaceType(mac),
                    OperatingSystem = os,
                    DiscoveredAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private string GuessOperatingSystem(int? ttl)
        {
            if (ttl == null) return "Unknown";
            if (ttl <= 64) return "Linux/Unix";
            if (ttl <= 128) return "Windows";
            return "Network Device";
        }
        private async Task<string> GetHostNameAsync(string ipAddress)
        {
            try
            {
                var host = await Dns.GetHostEntryAsync(ipAddress);
                return host?.HostName ?? ipAddress;
            }
            catch
            {
                return ipAddress;
            }
        }

        private string GetMacAddressForIp(string ipAddress)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var arpCommand = $"arp -a {ipAddress}";
                    var processInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c {arpCommand}")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(processInfo))
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var match = Regex.Match(output, @"([0-9A-F]{2}[:-]){5}([0-9A-F]{2})", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            return match.Value;
                        }
                    }
                }
                else
                {
                    var lines = File.ReadAllLines("/proc/net/arp");
                    foreach (var line in lines.Skip(1))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4 && parts[0] == ipAddress && parts[2] == "0x2")
                        {
                            return parts[3];
                        }
                    }
                }
            }
            catch
            {
            }

            return "Unknown";
        }

        private string DetermineInterfaceType(string macAddress)
        {
            if (macAddress == "Unknown")
                return "Unknown";

            if (macAddress.StartsWith("00:1A:2B") || macAddress.StartsWith("00:1E:65"))
                return "WiFi";

            return "Ethernet";
        }

        private IPAddress GetGatewayAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().GatewayAddresses)
                .Select(g => g?.Address)
                .FirstOrDefault(a => a != null && a.AddressFamily == AddressFamily.InterNetwork);
        }

        private string GetGatewayPrefix(string gateway)
        {
            int lastDot = gateway.LastIndexOf('.');
            if (lastDot >= 0)
                return gateway.Substring(0, lastDot + 1);
            return gateway;
        }
    }
}

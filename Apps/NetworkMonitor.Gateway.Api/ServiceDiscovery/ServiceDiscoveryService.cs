using System.Net.Sockets;
using System.Text;

namespace NetworkMonitor.Gateway.Api.ServiceDiscovery
{
    public class ServiceDiscoveryService : IServiceDiscoveryService
    {
        private const int MaxBannerLength = 256;

        // Protocols that speak first (SSH, SMTP, FTP) answer immediately; the rest wait for the
        // client, so the banner read gets its own short budget instead of the full connect timeout.
        private static readonly TimeSpan BannerTimeout = TimeSpan.FromMilliseconds(750);

        private readonly ILogger<ServiceDiscoveryService> _logger;
        private readonly int[] _ports;
        private readonly TimeSpan _probeTimeout;
        private readonly int _concurrency;

        public ServiceDiscoveryService(ILogger<ServiceDiscoveryService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var configuredPorts = configuration.GetSection("NetworkMonitor:ServiceScanPorts").Get<int[]>();
            _ports = configuredPorts is { Length: > 0 } ? configuredPorts : ServiceCatalog.DefaultPorts;

            var timeoutSeconds = configuration.GetValue<int?>("NetworkMonitor:ServiceProbeTimeoutSeconds") ?? 2;
            _probeTimeout = TimeSpan.FromSeconds(timeoutSeconds);

            _concurrency = configuration.GetValue<int?>("NetworkMonitor:ServiceScanConcurrency") ?? 32;
            if (_concurrency < 1)
                _concurrency = 1;
        }

        public async Task<IReadOnlyList<DiscoveredService>> ScanAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            using var gate = new SemaphoreSlim(_concurrency);

            var probes = _ports.Select(port => ProbePortAsync(ipAddress, port, gate, cancellationToken));
            var results = await Task.WhenAll(probes);

            return results.Where(result => result != null).Select(result => result!).ToList();
        }

        private async Task<DiscoveredService?> ProbePortAsync(
            string ipAddress,
            int port,
            SemaphoreSlim gate,
            CancellationToken cancellationToken)
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                using var client = new TcpClient();
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectCts.CancelAfter(_probeTimeout);

                await client.ConnectAsync(ipAddress, port, connectCts.Token);

                var banner = await ReadBannerAsync(client, cancellationToken);
                return new DiscoveredService(port, ServiceCatalog.NameFor(port), banner);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (SocketException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unexpected error probing {Ip}:{Port}", ipAddress, port);
                return null;
            }
            finally
            {
                gate.Release();
            }
        }

        private static async Task<string?> ReadBannerAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using var bannerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                bannerCts.CancelAfter(BannerTimeout);

                var buffer = new byte[MaxBannerLength];
                var read = await client.GetStream().ReadAsync(buffer, bannerCts.Token);
                if (read <= 0)
                    return null;

                var banner = Encoding.ASCII.GetString(buffer, 0, read).Trim();
                return banner.Length == 0 ? null : banner;
            }
            catch
            {
                return null;
            }
        }
    }
}

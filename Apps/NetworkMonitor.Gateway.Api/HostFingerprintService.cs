using System.Net.Sockets;
using System.Text;

namespace NetworkMonitor.Gateway.Api
{
    public record HostFingerprint(HashSet<int> RespondedPorts, string? SshBanner);

    public interface IHostFingerprintService
    {
        Task<HostFingerprint> FingerprintAsync(string ipAddress, CancellationToken cancellationToken = default);
    }

    public class HostFingerprintService : IHostFingerprintService
    {
        private static readonly int[] ProbePorts = { 22, 8006, 5000, 5001, 8123 };
        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

        private readonly ILogger<HostFingerprintService> _logger;

        public HostFingerprintService(ILogger<HostFingerprintService> logger)
        {
            _logger = logger;
        }

        public async Task<HostFingerprint> FingerprintAsync(string ipAddress, CancellationToken cancellationToken = default)
        {
            var respondedPorts = new HashSet<int>();
            string? sshBanner = null;

            var tasks = ProbePorts.Select(port => ProbePortAsync(ipAddress, port, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                if (!result.Open) continue;
                respondedPorts.Add(result.Port);
                if (result.Port == 22 && result.Banner != null)
                    sshBanner = result.Banner;
            }

            return new HostFingerprint(respondedPorts, sshBanner);
        }

        private async Task<(int Port, bool Open, string? Banner)> ProbePortAsync(string ipAddress, int port, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(ProbeTimeout);

                await client.ConnectAsync(ipAddress, port, cts.Token);

                string? banner = null;
                if (port == 22)
                    banner = await ReadBannerAsync(client, cts.Token);

                return (port, true, banner);
            }
            catch (OperationCanceledException)
            {
                return (port, false, null); 
            }
            catch (SocketException)
            {
                return (port, false, null); 
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unexpected error probing {Ip}:{Port}", ipAddress, port);
                return (port, false, null);
            }
        }

        private static async Task<string?> ReadBannerAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[256];
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read <= 0) return null;
                return Encoding.ASCII.GetString(buffer, 0, read).Trim();
            }
            catch
            {
                return null;
            }
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Gateway.Api
{
    public class JobExecutionWorker : BackgroundService
    {
        private readonly ILogger<JobExecutionWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public JobExecutionWorker(
            ILogger<JobExecutionWorker> logger,
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job Execution Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "A fatal error occurred while processing jobs.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessJobsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

            var activeJobs = await dbContext.MonitoringJobs
                .Include(j => j.Device)
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            var metricsToSave = new List<RawMetric>();

            foreach (var job in activeJobs)
            {
                var lastRun = job.LastRunAt ?? DateTime.MinValue;
                if ((DateTime.UtcNow - lastRun).TotalSeconds < job.IntervalSeconds)
                {
                    continue;
                }

                _logger.LogInformation($"Executing Job {job.Id} (Type {job.Type}) for Device {job.Device.IpAddress}");

                RawMetric metric = await ExecuteJobAsync(job, stoppingToken);
                metricsToSave.Add(metric);

                await dbContext.MonitoringJobs
                .Where(j => j.Id == job.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.LastRunAt, DateTime.UtcNow), stoppingToken);
            }

            if (metricsToSave.Any())
            {
                dbContext.RawMetrics.AddRange(metricsToSave);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task<RawMetric> ExecuteJobAsync(MonitoringJob job, CancellationToken stoppingToken)
        {
            var metric = new RawMetric
            {
                JobId = job.Id,
                Timestamp = DateTime.UtcNow,
                IsSuccess = false,
                Value = 0
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                switch (job.Type)
                {
                    case MonitoringJobType.IcmpPing:
                        metric = await ExecutePingCheck(job.Device.IpAddress, metric);
                        break;
                    case MonitoringJobType.Http:
                        metric = await ExecuteHttpCheck(job, metric, stoppingToken);
                        break;
                    case MonitoringJobType.TcpPort:
                        metric = await ExecuteTcpCheck(job, metric);
                        break;
                    default:
                        metric.ErrorMessage = $"Unknown job type: {job.Type}";
                        break;
                }
            }
            catch (Exception ex)
            {
                metric.IsSuccess = false;
                metric.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                if (metric.IsSuccess && metric.Value == 0)
                {
                    metric.Value = stopwatch.ElapsedMilliseconds;
                }
            }

            return metric;
        }

        private async Task<RawMetric> ExecutePingCheck(string ipAddress, RawMetric metric)
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 2000);

            if (reply.Status == IPStatus.Success)
            {
                metric.IsSuccess = true;
                metric.Value = reply.RoundtripTime;
            }
            else
            {
                metric.IsSuccess = false;
                metric.ErrorMessage = reply.Status.ToString();
            }

            return metric;
        }

        private async Task<RawMetric> ExecuteHttpCheck(MonitoringJob job, RawMetric metric, CancellationToken token)
        {
            var config = ExtractConfig(job.ConfigurationJson);
            var url = config.GetValueOrDefault("Url", $"http://{job.Device.IpAddress}");

            if (!await IsAllowedHttpTargetAsync(url, token))
            {
                metric.IsSuccess = false;
                metric.ErrorMessage = "Target URL is not permitted.";
                return metric;
            }

            var client = _httpClientFactory.CreateClient("MonitorClient");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url, token);

            metric.IsSuccess = response.IsSuccessStatusCode;
            metric.ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";

            return metric;
        }

        private static async Task<bool> IsAllowedHttpTargetAsync(string url, CancellationToken token)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            IEnumerable<IPAddress> addresses;
            if (IPAddress.TryParse(uri.Host, out var literal))
            {
                addresses = new[] { literal };
            }
            else
            {
                try
                {
                    addresses = await Dns.GetHostAddressesAsync(uri.Host, token);
                }
                catch
                {
                    return false;
                }
            }

            // Every resolved address must be a routable, non-internal host.
            return addresses.Any() && addresses.All(a => !IsInternalAddress(a));
        }

        private static bool IsInternalAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return true;

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
                    return true;
                if (address.IsIPv4MappedToIPv6)
                    address = address.MapToIPv4();
                else
                    return false;
            }

            var b = address.GetAddressBytes();
            if (b.Length != 4)
                return false;

            // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 169.254.0.0/16 (link-local incl. cloud metadata), 100.64.0.0/10 (CGNAT)
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                || b[0] == 0
                || b[0] == 127;
        }

        private async Task<RawMetric> ExecuteTcpCheck(MonitoringJob job, RawMetric metric)
        {
            var config = ExtractConfig(job.ConfigurationJson);
            if (!int.TryParse(config.GetValueOrDefault("Port", "80"), out int port))
            {
                metric.ErrorMessage = "Invalid port in configuration.";
                return metric;
            }

            using var tcpClient = new TcpClient();

            var connectTask = tcpClient.ConnectAsync(job.Device.IpAddress, port);
            if (await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask)
            {
                metric.IsSuccess = tcpClient.Connected;
            }
            else
            {
                metric.IsSuccess = false;
                metric.ErrorMessage = "Connection timed out.";
            }

            return metric;
        }

        private Dictionary<string, string> ExtractConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }
    }
}
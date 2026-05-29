using System.Diagnostics;
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

        // Keeps track of when each job was last executed so we respect the IntervalSeconds
        private readonly Dictionary<int, DateTime> _lastRunTimes = new();

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

                // Check for due jobs every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessJobsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NetworkMonitorDbContext>();

            // Fetch all jobs and include the Device so we know the IP Address
            var activeJobs = await dbContext.MonitoringJobs
                .Include(j => j.Device)
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            var metricsToSave = new List<RawMetric>();

            foreach (var job in activeJobs)
            {
                // Check if it's time to run this job based on its IntervalSeconds
                var lastRun = _lastRunTimes.GetValueOrDefault(job.Id, DateTime.MinValue);
                if ((DateTime.UtcNow - lastRun).TotalSeconds < job.IntervalSeconds)
                {
                    continue; // Skip, not time yet
                }

                _logger.LogInformation($"Executing Job {job.Id} (Type {job.Type}) for Device {job.Device.IpAddress}");

                RawMetric metric = await ExecuteJobAsync(job, stoppingToken);
                metricsToSave.Add(metric);

                // Update the last run time in memory
                _lastRunTimes[job.Id] = DateTime.UtcNow;
            }

            // Bulk save all new metrics to the database
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
                    case 1: // ICMP Ping
                        metric = await ExecutePingCheck(job.Device.IpAddress, metric);
                        break;
                    case 2: // HTTP/HTTPS
                        metric = await ExecuteHttpCheck(job, metric, stoppingToken);
                        break;
                    case 3: // TCP Port
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
                // If it's a success but we didn't explicitly set a Value (like ping latency), use the stopwatch time
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
            var reply = await ping.SendPingAsync(ipAddress, 2000); // 2 second timeout

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
            // Parse ConfigurationJson to get the specific URL (e.g., {"Url": "https://192.168.178.50:8006"})
            var config = ExtractConfig(job.ConfigurationJson);
            var url = config.GetValueOrDefault("Url", $"http://{job.Device.IpAddress}");

            var client = _httpClientFactory.CreateClient("MonitorClient");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(url, token);

            metric.IsSuccess = response.IsSuccessStatusCode;
            metric.ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";

            return metric;
        }

        private async Task<RawMetric> ExecuteTcpCheck(MonitoringJob job, RawMetric metric)
        {
            // Parse ConfigurationJson for the port (e.g., {"Port": "22"} for SSH)
            var config = ExtractConfig(job.ConfigurationJson);
            if (!int.TryParse(config.GetValueOrDefault("Port", "80"), out int port))
            {
                metric.ErrorMessage = "Invalid port in configuration.";
                return metric;
            }

            using var tcpClient = new TcpClient();

            // Connect asynchronously with a 3-second timeout
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
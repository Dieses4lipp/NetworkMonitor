using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NetworkMonitor.Domain;

namespace NetworkMonitor.Gateway.Api.PlatformInspection
{
    public class ProxmoxInspector : IPlatformInspector
    {
        public PlatformType SupportedPlatform => PlatformType.ProxmoxVe;
        public string Source => "Proxmox";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProxmoxOptions _options;
        private readonly ILogger<ProxmoxInspector> _logger;

        public ProxmoxInspector(
            IHttpClientFactory httpClientFactory,
            IOptions<ProxmoxOptions> options,
            ILogger<ProxmoxInspector> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<HostInspectionResult> InspectAsync(Device device, CancellationToken cancellationToken)
        {
            var hostOptions = _options.Hosts.FirstOrDefault(h => h.HostIpAddress == device.IpAddress);
            if (hostOptions == null)
                return HostInspectionResult.Failed($"No Proxmox API token configured for host {device.IpAddress}");

            var client = _httpClientFactory.CreateClient("ProxmoxClient");
            client.BaseAddress = new Uri($"https://{device.IpAddress}:8006/api2/json/");
            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization", $"PVEAPIToken={hostOptions.TokenId}={hostOptions.TokenSecret}");

            try
            {
                var nodes = await GetDataArrayAsync(client, "nodes", cancellationToken);
                var workloads = new List<WorkloadInfo>();
                var services = new List<ServiceUnitInfo>();

                foreach (var node in nodes)
                {
                    var nodeName = node.GetProperty("node").GetString();
                    if (nodeName == null) continue;

                    workloads.AddRange(await GetWorkloadsAsync(client, nodeName, "lxc", "LXC", cancellationToken));
                    workloads.AddRange(await GetWorkloadsAsync(client, nodeName, "qemu", "VM", cancellationToken));
                    services.AddRange(await GetServicesAsync(client, nodeName, cancellationToken));
                }

                return new HostInspectionResult(true, workloads, services);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Proxmox inspection failed for {Ip}", device.IpAddress);
                return HostInspectionResult.Failed(ex.Message);
            }
        }

        private static async Task<List<JsonElement>> GetDataArrayAsync(HttpClient client, string relativeUrl, CancellationToken cancellationToken)
        {
            using var response = await client.GetAsync(relativeUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return doc.RootElement.GetProperty("data").EnumerateArray().Select(e => e.Clone()).ToList();
        }

        private static async Task<List<WorkloadInfo>> GetWorkloadsAsync(HttpClient client, string node, string guestType, string kind, CancellationToken cancellationToken)
        {
            var guests = await GetDataArrayAsync(client, $"nodes/{node}/{guestType}", cancellationToken);
            var result = new List<WorkloadInfo>();
            foreach (var guest in guests)
            {
                var vmid = guest.GetProperty("vmid").ToString();
                var name = guest.TryGetProperty("name", out var n) ? n.GetString() ?? vmid : vmid;
                var status = guest.TryGetProperty("status", out var s) ? s.GetString() ?? "unknown" : "unknown";
                result.Add(new WorkloadInfo(vmid, name, kind, status));
            }
            return result;
        }

        private static async Task<List<ServiceUnitInfo>> GetServicesAsync(HttpClient client, string node, CancellationToken cancellationToken)
        {
            var svcs = await GetDataArrayAsync(client, $"nodes/{node}/services", cancellationToken);
            var result = new List<ServiceUnitInfo>();
            foreach (var svc in svcs)
            {
                var name = svc.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (name == null) continue;
                var state = svc.TryGetProperty("state", out var st) ? st.GetString() ?? "unknown" : "unknown";
                result.Add(new ServiceUnitInfo(name, state, null));
            }
            return result;
        }
    }
}

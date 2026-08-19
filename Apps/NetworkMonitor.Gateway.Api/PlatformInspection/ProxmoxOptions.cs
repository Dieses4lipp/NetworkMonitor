namespace NetworkMonitor.Gateway.Api.PlatformInspection
{
    public class ProxmoxOptions
    {
        public List<ProxmoxHostOptions> Hosts { get; set; } = new();
    }

    public class ProxmoxHostOptions
    {
        public string HostIpAddress { get; set; } = null!;

        public string TokenId { get; set; } = null!;

        public string TokenSecret { get; set; } = null!;
    }
}

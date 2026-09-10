namespace NetworkMonitor.Gateway.Api
{
    public static class ServiceCatalog
    {
        public const string UnknownServiceName = "Unknown";

        public static IReadOnlyDictionary<int, string> ByPort { get; } = new Dictionary<int, string>
        {
            [21] = "FTP",
            [KnownServicePorts.Ssh] = "SSH",
            [23] = "Telnet",
            [25] = "SMTP",
            [53] = "DNS",
            [80] = "HTTP",
            [110] = "POP3",
            [111] = "rpcbind",
            [135] = "MSRPC",
            [139] = "NetBIOS-SSN",
            [143] = "IMAP",
            [443] = "HTTPS",
            [445] = "SMB",
            [465] = "SMTPS",
            [548] = "AFP",
            [587] = "SMTP Submission",
            [631] = "IPP",
            [993] = "IMAPS",
            [995] = "POP3S",
            [1433] = "MSSQL",
            [1883] = "MQTT",
            [2049] = "NFS",
            [3000] = "HTTP (alt)",
            [3306] = "MySQL",
            [3389] = "RDP",
            [KnownServicePorts.SynologyHttp] = "Synology DSM",
            [KnownServicePorts.SynologyHttps] = "Synology DSM (HTTPS)",
            [5432] = "PostgreSQL",
            [5900] = "VNC",
            [6379] = "Redis",
            [KnownServicePorts.ProxmoxVe] = "Proxmox VE",
            [8080] = "HTTP (alt)",
            [KnownServicePorts.HomeAssistant] = "Home Assistant",
            [8443] = "HTTPS (alt)",
            [9000] = "Portainer",
            [9090] = "Prometheus",
            [27017] = "MongoDB",
            [32400] = "Plex",
        };

        public static int[] DefaultPorts { get; } = ByPort.Keys.OrderBy(port => port).ToArray();

        public static string NameFor(int port) =>
            ByPort.TryGetValue(port, out var name) ? name : UnknownServiceName;
    }
}

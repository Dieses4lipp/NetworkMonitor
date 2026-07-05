using System.Reflection;

namespace NetworkMonitor.Gateway.Api
{
    public interface IVendorLookupService
    {
        string? Lookup(string macAddress);
    }

    public class VendorLookupService : IVendorLookupService
    {
        private Dictionary<string, string> _ouiMap = new();
        private readonly ILogger<VendorLookupService> _logger;

        public VendorLookupService(ILogger<VendorLookupService> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(30);
                var csv = await http.GetStringAsync(
                    "https://standards-oui.ieee.org/oui/oui.csv");

                var map = new Dictionary<string, string>();
                foreach (var line in csv.Split('\n').Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                        map[parts[1].Trim().ToUpperInvariant()] = parts[2].Trim('"', ' ');
                }

                _ouiMap = map;
                _logger.LogInformation("OUI database loaded: {Count} entries", map.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load OUI database — vendor lookup will be unavailable");
            }
        }

        public string? Lookup(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress) || macAddress == "Unknown")
                return null;

            var prefix = macAddress.Replace(":", "").Replace("-", "")
                                   .ToUpperInvariant();
            return prefix.Length >= 6
                ? _ouiMap.GetValueOrDefault(prefix[..6])
                : null;
        }
    }
}
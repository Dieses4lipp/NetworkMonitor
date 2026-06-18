using System.Reflection;

namespace NetworkMonitor.Gateway.Api
{
    public interface IVendorLookupService
    {
        string? Lookup(string macAddress);
    }

    public class VendorLookupService : IVendorLookupService
    {
        private readonly Dictionary<string, string> _ouiMap;

        public VendorLookupService()
        {
            _ouiMap = LoadOuiMap();
        }

        public string? Lookup(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress) || macAddress == "Unknown")
                return null;

            var prefix = macAddress.Replace(":", "").Replace("-", "").ToUpperInvariant();
            if (prefix.Length < 6) return null;

            return _ouiMap.GetValueOrDefault(prefix[..6]);
        }

        private static Dictionary<string, string> LoadOuiMap()
        {
            var map = new Dictionary<string, string>();
            using var stream = 
            if (stream == null) return map;

            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',', 2);
                if (parts.Length == 2)
                    map[parts[0].Trim().ToUpperInvariant()] = parts[1].Trim();
            }
            return map;
        }
    }
}

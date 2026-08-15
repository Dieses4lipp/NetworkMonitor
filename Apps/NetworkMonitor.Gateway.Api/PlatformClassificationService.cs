using NetworkMonitor.Domain;

namespace NetworkMonitor.Gateway.Api
{
    public record PlatformClassificationInput(
        string? Vendor,
        string? Hostname,
        string? OperatingSystemGuess,
        string? SshBanner = null,
        IReadOnlySet<int>? RespondedPorts = null);

    public interface IPlatformClassificationService
    {
        PlatformType Classify(PlatformClassificationInput input);
    }

    public class PlatformClassificationService : IPlatformClassificationService
    {
        private static readonly (Func<PlatformClassificationInput, bool> Matches, PlatformType Platform)[] Rules =
        {
            (i => i.RespondedPorts?.Contains(8006) == true, PlatformType.ProxmoxVe),
            (i => i.RespondedPorts?.Contains(5000) == true || i.RespondedPorts?.Contains(5001) == true, PlatformType.Synology),
            (i => i.RespondedPorts?.Contains(8123) == true, PlatformType.HomeAssistant),

            (i => ContainsIgnoreCase(i.SshBanner, "ubuntu"), PlatformType.Ubuntu),
            (i => ContainsIgnoreCase(i.SshBanner, "debian"), PlatformType.Debian),

            (i => ContainsIgnoreCase(i.Vendor, "synology"), PlatformType.Synology),
            (i => ContainsIgnoreCase(i.Vendor, "apple") && ContainsIgnoreCase(i.OperatingSystemGuess, "macos"), PlatformType.MacOS),
            (i => ContainsIgnoreCase(i.Vendor, "apple"), PlatformType.MacOS),

            (i => ContainsIgnoreCase(i.OperatingSystemGuess, "windows"), PlatformType.Windows),
            (i => ContainsIgnoreCase(i.OperatingSystemGuess, "linux"), PlatformType.LinuxGeneric),
        };

        public PlatformType Classify(PlatformClassificationInput input)
        {
            foreach (var rule in Rules)
            {
                if (rule.Matches(input))
                    return rule.Platform;
            }

            return PlatformType.Unknown;
        }

        private static bool ContainsIgnoreCase(string? haystack, string needle) =>
            haystack != null && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}

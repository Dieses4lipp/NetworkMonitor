using NetworkMonitor.Domain;

namespace NetworkMonitor.Gateway.Api.PlatformInspection
{
    public record WorkloadInfo(string ExternalId, string Name, string Kind, string Status);

    public record ServiceUnitInfo(string UnitName, string ActiveState, string? SubState);

    public record HostInspectionResult(
        bool WasSuccessful,
        IReadOnlyList<WorkloadInfo> Workloads,
        IReadOnlyList<ServiceUnitInfo> Services,
        string? ErrorMessage = null)
    {
        public static HostInspectionResult Failed(string errorMessage) =>
            new(false, Array.Empty<WorkloadInfo>(), Array.Empty<ServiceUnitInfo>(), errorMessage);
    }

    public interface IPlatformInspector
    {
        PlatformType SupportedPlatform { get; }

        string Source { get; }

        Task<HostInspectionResult> InspectAsync(Device device, CancellationToken cancellationToken);
    }
}

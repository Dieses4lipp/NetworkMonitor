using NetworkMonitor.Domain;

namespace NetworkMonitor.Application.MonitoringJobs;

public record CreateJobRequest(
    int DeviceId,
    MonitoringJobType Type,
    int IntervalSeconds,
    string? ConfigurationJson);

public record UpdateJobRequest(
    MonitoringJobType? Type,
    int? IntervalSeconds,
    string? ConfigurationJson);

public record CreateJobResult(bool DeviceExists, MonitoringJob? Job);

public interface IMonitoringJobService
{
    Task<CreateJobResult> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken = default);

    Task<MonitoringJob?> UpdateJobAsync(int id, UpdateJobRequest request, CancellationToken cancellationToken = default);

    Task<MonitoringJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> DeleteJobAsync(int id, CancellationToken cancellationToken = default);
}

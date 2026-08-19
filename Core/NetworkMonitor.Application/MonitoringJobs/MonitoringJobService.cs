using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Application.MonitoringJobs;

public class MonitoringJobService : IMonitoringJobService
{
    private readonly NetworkMonitorDbContext _dbContext;

    public MonitoringJobService(NetworkMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateJobResult> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken = default)
    {
        var deviceExists = await _dbContext.Devices.AnyAsync(d => d.Id == request.DeviceId, cancellationToken);
        if (!deviceExists)
            return new CreateJobResult(false, null);

        var job = new MonitoringJob
        {
            DeviceId = request.DeviceId,
            Type = request.Type,
            IntervalSeconds = request.IntervalSeconds,
            ConfigurationJson = request.ConfigurationJson
        };

        _dbContext.MonitoringJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateJobResult(true, job);
    }

    public async Task<MonitoringJob?> UpdateJobAsync(int id, UpdateJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.MonitoringJobs.FindAsync(new object?[] { id }, cancellationToken);
        if (job == null)
            return null;

        if (request.IntervalSeconds.HasValue)
            job.IntervalSeconds = request.IntervalSeconds.Value;

        if (request.ConfigurationJson != null)
            job.ConfigurationJson = request.ConfigurationJson;

        if (request.Type.HasValue)
            job.Type = request.Type.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return job;
    }

    public async Task<MonitoringJob?> GetJobByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonitoringJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteJobAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.MonitoringJobs.FindAsync(new object?[] { id }, cancellationToken);
        if (job == null)
            return false;

        _dbContext.MonitoringJobs.Remove(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Domain;
using NetworkMonitor.Infrastructure.Data.Context;

namespace NetworkMonitor.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class MonitoringJobController : ControllerBase
    {
        private readonly NetworkMonitorDbContext _dbContext;
        private readonly ILogger<MonitoringJobController> _logger;

        public MonitoringJobController(NetworkMonitorDbContext dbContext, ILogger<MonitoringJobController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewJob([FromBody] CreateJobRequest request)
        {
            try
            {
                // Verify the device actually exists first
                var deviceExists = await _dbContext.Devices.AnyAsync(d => d.Id == request.DeviceId);
                if (!deviceExists)
                    return BadRequest($"Device with ID {request.DeviceId} not found.");

                var job = new MonitoringJob
                {
                    DeviceId = request.DeviceId,
                    Type = request.Type,
                    IntervalSeconds = request.IntervalSeconds,
                    ConfigurationJson = request.ConfigurationJson
                };

                _dbContext.MonitoringJobs.Add(job);
                await _dbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new monitoring job");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateJobById(int id, [FromBody] UpdateJobRequest request)
        {
            try
            {
                var job = await _dbContext.MonitoringJobs.FindAsync(id);
                if (job == null)
                    return NotFound($"Job with ID {id} not found.");

                // Only update the fields that were provided
                if (request.IntervalSeconds.HasValue)
                    job.IntervalSeconds = request.IntervalSeconds.Value;

                if (request.ConfigurationJson != null)
                    job.ConfigurationJson = request.ConfigurationJson;
                if (request.Type.HasValue)
                    job.Type = request.Type.Value;

                await _dbContext.SaveChangesAsync();

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating job {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            var job = await _dbContext.MonitoringJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();
            return Ok(job);
        }

        [HttpGet("types")]
        public IActionResult GetJobTypes()
        {
            // Since Type is an integer in your database, we can define the available types here.
            // Later, you could move these to an Enum in your Domain project.
            var types = new List<object>
            {
                new { Id = 1, Name = "ICMP Ping", Description = "Standard network ping" },
                new { Id = 2, Name = "HTTP/HTTPS", Description = "Web endpoint health check" },
                new { Id = 3, Name = "TCP Port", Description = "Check if a specific port is open" },
                new { Id = 4, Name = "SNMP", Description = "SNMP metric extraction" }
            };

            return Ok(types);
        }
    }
    public record CreateJobRequest(
        int DeviceId,
        int Type,
        int IntervalSeconds,
        string? ConfigurationJson
    );

    public record UpdateJobRequest(
        int? Type,
        int? IntervalSeconds,
        string? ConfigurationJson
    );
}
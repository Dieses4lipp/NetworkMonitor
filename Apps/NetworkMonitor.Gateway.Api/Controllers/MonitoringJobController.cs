using Microsoft.AspNetCore.Mvc;
using NetworkMonitor.Application.MonitoringJobs;
using NetworkMonitor.Domain;

namespace NetworkMonitor.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class MonitoringJobController : ControllerBase
    {
        private readonly IMonitoringJobService _jobService;
        private readonly ILogger<MonitoringJobController> _logger;

        public MonitoringJobController(IMonitoringJobService jobService, ILogger<MonitoringJobController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewJob([FromBody] CreateJobRequest request)
        {
            try
            {
                var result = await _jobService.CreateJobAsync(request);
                if (!result.DeviceExists)
                    return BadRequest($"Device with ID {request.DeviceId} not found.");

                return CreatedAtAction(nameof(GetJobById), new { id = result.Job!.Id }, result.Job);
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
                var job = await _jobService.UpdateJobAsync(id, request);
                if (job == null)
                    return NotFound($"Job with ID {id} not found.");

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job {JobId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            try
            {
                var job = await _jobService.GetJobByIdAsync(id);
                if (job == null) return NotFound();
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job {JobId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("types")]
        public IActionResult GetJobTypes()
        {
            var types = Enum.GetValues<MonitoringJobType>()
            .Select(t => new { Id = (int)t, Name = t.ToString() });

            return Ok(types);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobById(int id)
        {
            try
            {
                var deleted = await _jobService.DeleteJobAsync(id);
                if (!deleted)
                    return NotFound($"Job with ID {id} not found.");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

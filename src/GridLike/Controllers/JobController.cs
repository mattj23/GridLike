using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GridLike.Auth.Api;
using GridLike.Data;
using GridLike.Auth;
using GridLike.Data.Models;
using GridLike.Services;
using GridLike.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GridLike.Controllers
{
    [Route("api/jobs")]
    [Authorize(AuthenticationSchemes = Schemes.ApiKey)]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IStorageProvider _storage;
        private readonly JobDataStore _jobs;
        private readonly ILogger<JobController> _logger;

        public JobController(JobDataStore jobs, IStorageProvider storage, ILogger<JobController> logger)
        {
            _logger = logger;
            _jobs = jobs;
            _storage = storage;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var jobs = await _jobs.GetAllViews();
            return Ok(jobs);
        }

        [HttpPost("{jobKey}/delete")]
        public async Task<IActionResult> Remove(string jobKey)
        {
            var job = await _jobs.GetJob(jobKey);
            if (job is null)
            {
                return NotFound();
            }
            
            // Remove the job binary payloads
            var inputName = $"{job.Id}.job";
            var outputName = $"{job.Id}.result";

            await _storage.DeleteFile(inputName);
            await _storage.DeleteFile(outputName);
            
            // Remove the job itself
            await _jobs.RemoveJob(job);

            return Accepted(new { deleted = job.Key });
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromForm] JobSubmit submit)
        {
            // Validate the submission
            JobPriority priority;
            if (submit.Priority == "immediate")
            {
                priority = JobPriority.Immediate;
            }
            else if (submit.Priority == "batch")
            {
                priority = JobPriority.Batch;
            }
            else
            {
                return BadRequest(new
                    { problem = $"Unrecognized priority '{submit.Priority}', use either 'immediate' or 'batch'" });
            }

            var jobId = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(submit.Key))
            {
                submit.Key = jobId.ToString();
            }

            // Store data to backend
            var storeName = $"{jobId}.job";
            try
            {
                await _storage.PutFile(storeName, submit.Payload.OpenReadStream(), submit.Payload.Length);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while writing job to storage backend");
                return this.Problem();
            }
            
            try
            {
                await _jobs.AddJob(new Job
                {
                    Id = jobId,
                    Key = submit.Key,
                    Display = submit.Description,
                    Priority = priority,
                    Submitted = DateTime.UtcNow,
                    Status = JobStatus.Pending
                });
            }
            catch (DuplicateNameException e)
            {
                return BadRequest(new
                    { problem = $"A job with the key {submit.Key} already exists" });
            }
            
            return Accepted(new { created = submit.Key, id = jobId });
        }

        public class JobSubmit
        {
            public IFormFile Payload { get; set; } = null!;

            public string Priority { get; set; } = null!;

            public string? Key { get; set; }
            
            public string? Description { get; set; }
        }
    }
}
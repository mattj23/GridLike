using GridLike.Data;
using GridLike.Data.Models;
using GridLike.Models;

namespace GridLike.Services;

/// <summary>
/// The JobRegistry is the data store for managing everything relating to the persistence of job metadata.
/// </summary>
public class JobRegistry
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServerConfiguration _config;
    private readonly ILogger<JobRegistry> _logger;

    private readonly SemaphoreSlim _guard;
    
    public JobRegistry(IServiceScopeFactory scopeFactory, ServerConfiguration config, ILogger<JobRegistry> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
        _guard = new SemaphoreSlim(0, 1);
    }

    public Task<Guid> CreateNewJob(string? key, string? type, JobPriority priority)
    {
        throw new NotImplementedException();
    }

    public Task<Job> GetJob(string jobKey)
    {
        throw new NotImplementedException();
    }

    public Task SetJobStatus(Guid id, JobStatus newStatus)
    {
        throw new NotImplementedException();
    }

    public Task DeleteJob(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Job[]> GetBatchJobs(int batchid)
    {
        throw new NotImplementedException();
    }

}
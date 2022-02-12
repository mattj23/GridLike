using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GridLike.Data;
using GridLike.Data.Models;
using GridLike.Data.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridLike.Services
{
    public class JobDataStore
    {
        private readonly ConcurrentDictionary<Guid, Job> _jobs;
        private readonly ConcurrentDictionary<string, Guid> _jobKeys;
        private readonly ILogger<JobDataStore> _logger;
        private readonly IScheduler _scheduler;

        private bool _firstLoad;

        private readonly Subject<ViewUpdate<JobView>> _jobUpdates;

        private readonly IServiceScopeFactory _scopeFactory;

        public JobDataStore(IServiceScopeFactory scopeFactory, ILogger<JobDataStore> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jobs = new ConcurrentDictionary<Guid, Job>();
            _jobKeys = new ConcurrentDictionary<string, Guid>();
            _jobUpdates = new Subject<ViewUpdate<JobView>>();
            _scheduler = new EventLoopScheduler();
        }

        public IObservable<ViewUpdate<JobView>> JobUpdates => _jobUpdates.AsObservable();
        
        public int TotalJobs { get; private set; }
        public int CompletedJobs { get; private set; }

        public async Task<JobView[]> GetAllViews()
        {
            await LoadAllJobs();
            return _jobs.Values.Select(j => j.ToView()).ToArray();
        }

        public async Task<Job?> GetJob(string jobKey)
        {
            await LoadAllJobs();
            
            // TODO: remove this if RemoveJob is the only thing that uses it
            
            if (!_jobKeys.ContainsKey(jobKey)) return null;

            var jobId = _jobKeys[jobKey];
            if (!_jobs.ContainsKey(jobId)) return null;
            return _jobs[jobId];
        }

        /// <summary>
        /// Finds and reserves a job for a worker. If successful, returns a view of the job.
        /// </summary>
        /// <returns></returns>
        public Task<JobView?> PickJobAndSetRunning()
        {
            var source = new TaskCompletionSource<JobView?>();

            _scheduler.Schedule(async () =>
            {
                var target = NextJobUnsafe();
                if (target is null)
                {
                    source.TrySetResult(null);
                    return;
                }
                _logger.LogDebug("Job '{0}' reserved and set as running", target.Key);
                
                // Set the local job value
                target.Status = JobStatus.Running;
                target.Started = DateTime.UtcNow;
                _jobUpdates.OnNext(target.ToView().ToUpdate(UpdateType.Update));
                
                // TODO: should this change be pushed to the database?
                source.TrySetResult(target.ToView());
            });

            return source.Task;
        }

        public Task SetJobPending(Guid jobId)
        {
            var source = new TaskCompletionSource();
            _scheduler.Schedule(async () =>
            {
                var retrieved = _jobs.TryGetValue(jobId, out var job);
                if (!retrieved || job is null)
                {
                    source.TrySetResult();
                    return;
                }
                
                job.Status = JobStatus.Pending;
                _jobUpdates.OnNext(job.ToView().ToUpdate(UpdateType.Update));
                
                using var scope = _scopeFactory.CreateScope();
                await using var context = scope.ServiceProvider.GetService<GridLikeContext>();
                if (context is null)
                {
                    throw new Exception("Couldn't get database context from service collection");
                }

                var jobItem = await context.Jobs.FindAsync(jobId);
                jobItem.Status = JobStatus.Pending;
                await context.SaveChangesAsync();
                
                source.TrySetResult();
            });

            return source.Task;
        }

        public Task SetJobDone(Guid jobId)
        {
            var source = new TaskCompletionSource();
            _scheduler.Schedule(async () =>
            {
                var job = _jobs[jobId];
                job.Status = JobStatus.Done;
                job.Ended = DateTime.UtcNow;
                CompletedJobs++;
                _jobUpdates.OnNext(job.ToView().ToUpdate(UpdateType.Add));
                
                using var scope = _scopeFactory.CreateScope();
                await using var context = scope.ServiceProvider.GetService<GridLikeContext>();
                if (context is null)
                {
                    throw new Exception("Couldn't get database context from service collection");
                }

                var jobItem = await context.Jobs.FindAsync(jobId);
                jobItem.Status = JobStatus.Done;
                jobItem.Started = job.Started;
                jobItem.Ended = job.Ended;
                await context.SaveChangesAsync();
                
                source.TrySetResult();
            });

            return source.Task;
        }
        
        /// <summary>
        /// Removes a job from the store. Safe to be called from any thread.
        /// </summary>
        /// <param name="job">The job to remove, identified by its GUID</param>
        /// <returns></returns>
        public Task RemoveJob(Job job)
        {
            var source = new TaskCompletionSource();
            _scheduler.Schedule(async () =>
            {
                await this.RemoveJobUnsafe(job);
                source.TrySetResult();
            });

            return source.Task;
        }

        /// <summary>
        /// Adds a job to the store. Safe to be called from any thread.
        /// </summary>
        /// <param name="job">A new job with a unique GUID to be added to the store</param>
        /// <exception cref="DuplicateNameException">Thrown if the key is specified but not unique</exception>
        /// <exception cref="Exception">General exception on database failure</exception>
        public async Task AddJob(Job job)
        {
            /* This should be safe to be called from any thread because all other operations which would be performed
             on the job will not yet know that it exists. */
            if (_jobKeys.ContainsKey(job.Key))
            {
                throw new DuplicateNameException();
            }
            
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetService<GridLikeContext>();
            if (context is null)
            {
                throw new Exception("Couldn't get database context from service collection");
            }

            _jobKeys[job.Key] = job.Id;
            _jobs[job.Id] = job;
            try
            {
                await context.Jobs.AddAsync(job);
                await context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error adding job to database");
                _jobKeys.TryRemove(job.Key, out var _);
                _jobs.TryRemove(job.Id, out var _);
                throw;
            }

            TotalJobs++;
            _jobUpdates.OnNext(job.ToView().ToUpdate(UpdateType.Add));
        }
        
        private async Task RemoveJobUnsafe(Job job)
        {
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetService<GridLikeContext>();
            if (context is null)
            {
                throw new Exception("Couldn't get database context from service collection");
            }
            
            _jobKeys.Remove(job.Key, out var _);
            _jobs.Remove(job.Id, out var _);

            var fetched = await context.Jobs.FindAsync(job.Id);
            if (fetched is not null)
            {
                context.Jobs.Remove(fetched);
                await context.SaveChangesAsync();
            }

            TotalJobs = _jobs.Count;
            CompletedJobs = _jobs.Values.Count(j => j.Status == JobStatus.Done);
            _jobUpdates.OnNext(job.ToView().ToUpdate(UpdateType.Delete));
        }

        /// <summary>
        /// Finds the next job to be run according to the various priority rules. This should only be run from the
        /// single threaded event loop scheduler.
        /// </summary>
        /// <returns></returns>
        private Job? NextJobUnsafe()
        {
            // TODO: These should be stored in ordered collections to avoid the repeated sorting and searching
            var pending = _jobs.Values.Where(j => j.Status == JobStatus.Pending).ToArray();
            
            // Any immediate jobs?
            var immediate = pending.Where(j => j.Priority == JobPriority.Immediate)
                .OrderByDescending(x => x.Age()).FirstOrDefault();

            if (immediate is not null) return immediate;

            // Batch jobs
            return pending.OrderByDescending(x => x.Age()).FirstOrDefault();
        }

        private async Task LoadAllJobs()
        {
            if (_firstLoad) return;
            
            using var scope = _scopeFactory.CreateScope();
            await using var context = scope.ServiceProvider.GetService<GridLikeContext>();
            if (context is null)
            {
                throw new Exception("Couldn't get database context from service collection");
            }

            foreach (var job in await context.Jobs.ToListAsync())
            {
                _jobs[job.Id] = job;
                _jobKeys[job.Key] = job.Id;
            }

            TotalJobs = _jobs.Count;
            CompletedJobs = _jobs.Values.Count(j => j.Status == JobStatus.Done);

            _firstLoad = true;
        }
    }
}
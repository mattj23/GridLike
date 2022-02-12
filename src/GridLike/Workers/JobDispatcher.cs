using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using GridLike.Data.Views;
using GridLike.Services;
using GridLike.Services.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GridLike.Workers
{
    public class JobDispatcher
    {
        private readonly ConcurrentDictionary<Guid, Worker> _waitingWorkers;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobDispatcher> _logger;
        private readonly IScheduler _scheduler;
        private readonly JobDataStore _jobStore;
        private readonly ConcurrentDictionary<Guid, ActiveJob> _active;
        private readonly Subject<Guid> _binaryRecieved;

        public JobDispatcher(IServiceScopeFactory scopeFactory, JobDataStore jobStore, ILogger<JobDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jobStore = jobStore;
            
            _waitingWorkers = new ConcurrentDictionary<Guid, Worker>();
            _active = new ConcurrentDictionary<Guid, ActiveJob>();
            _binaryRecieved = new Subject<Guid>();
            
            _scheduler = new EventLoopScheduler();
            
            // We will use the job store's notification mechanism to look for new jobs being added 
            _jobStore.JobUpdates.Where(u => u.Type == UpdateType.Add)
                .ObserveOn(_scheduler)
                .Subscribe(_ => DispatchAllAvailable());

            Observable.Interval(TimeSpan.FromSeconds(5))
                .ObserveOn(_scheduler)
                .Subscribe(_ => DispatchAllAvailable());
        }

        public IObservable<Guid> BinaryReceived => _binaryRecieved.AsObservable();

        public void AddReadyWorker(Worker worker)
        {
            _scheduler.Schedule(() => AddReadyWorkerUnsafe(worker));
        }

        public void NotifyWorkerDisconnect(Guid workerId)
        {
            _scheduler.Schedule(() => CheckUnexpectedCompletion(workerId));
        }

        /// <summary>
        /// Checks for and handles a worker's unexpected state transition while a job is still considered active. This
        /// method must only run on the single threaded scheduler.
        /// </summary>
        /// <param name="workerId"></param>
        private async Task CheckUnexpectedCompletion(Guid workerId)
        {
            if (_active.ContainsKey(workerId))
            {
                _active.TryRemove(workerId, out var activeJob);
                if (activeJob is null) return;
                await _jobStore.SetJobPending(activeJob.JobId);
            }
            
        }
        
        private async void AddReadyWorkerUnsafe(Worker worker)
        {
            if (worker.State != WorkerState.Ready) return;
            await this.CheckUnexpectedCompletion(worker.Id);

            _waitingWorkers[worker.Id] = worker;
            _scheduler.Schedule(() => DispatchAllAvailable());
        }

        private async void ReceiveBinary(Guid workerId, byte[] payload)
        {
            _logger.LogDebug("Received binary back from worker {0}", workerId);
            
            // Attempt to find the job that this worker was working on
            bool retrieved = _active.TryRemove(workerId, out var activeJob);
            if (!retrieved) return;
            
            activeJob.BinarySubscription.Dispose();
            
            using var scope = _scopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetService<IStorageProvider>();
            var jobResultName = $"{activeJob.JobId}.result";

            using var memStream = new MemoryStream();
            await memStream.WriteAsync(payload);
            memStream.Seek(0, 0);
            storage.PutFile(jobResultName, memStream, memStream.Length);

            await _jobStore.SetJobDone(activeJob.JobId);
            _binaryRecieved.OnNext(workerId);
        }

        private async Task<bool> DispatchOne()
        {
            if (_waitingWorkers.IsEmpty) return false;
            
            var reserved = await _jobStore.PickJobAndSetRunning();
            if (reserved is null) return false;
            
            var worker = _waitingWorkers.Values.First();
            _waitingWorkers.TryRemove(worker.Id, out worker);
            if (worker is null)
            {
                // How could this have happened?
                throw new KeyNotFoundException("This shouldn't have happened");
            }

            worker.SetBusy();
            _active[worker.Id] = new ActiveJob
            {
                JobId = reserved.Id,
                BinarySubscription = worker.BinaryMessages.Subscribe(t => ReceiveBinary(t.Item1, t.Item2))
            };
            
            _logger.LogDebug("Scheduling transfer of {0} to {1}", reserved.Key, worker.Name);
            TaskPoolScheduler.Default.Schedule(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var storage = scope.ServiceProvider.GetService<IStorageProvider>();
                var jobDataName = $"{reserved.Id}.job";
                
                // TODO: at some point this should be a buffered pass-through instead of copying the whole payload into
                // memory at once, but for now I wasn't able to get that to work reliably because I'm not sure how 
                // minio is handling the callback
                await using var memstrm = new MemoryStream();
                await storage.GetFile(jobDataName, s => s.CopyTo(memstrm));
                await worker.SendBytes(memstrm.ToArray());
            });

            return true;
        }
        
        private async Task DispatchAllAvailable()
        {
            while (await DispatchOne()) { }
        }

        private record ActiveJob
        {
            public IDisposable BinarySubscription { get; init; }
            public Guid JobId { get; init; }
        }
    }
}
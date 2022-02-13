using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using GridLike.Data;
using GridLike.Models;
using GridLike.Models.Views;
using GridLike.Services.Storage;
using GridLike.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridLike.Services
{
    public class WorkerManager : IHostedService, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, Worker> _workers;
        private IDisposable? _tickSubscription;
        private readonly IScheduler _scheduler;
        private readonly Subject<ViewUpdate<WorkerView>> _updates;
        private readonly JobDispatcher _dispatcher;
        private readonly ILogger<WorkerManager> _logger;
        private readonly IWorkerAuthenticator _workerAuthenticator;

        public WorkerManager(JobDispatcher dispatcher, IWorkerAuthenticator workerAuthenticator,
            ILogger<WorkerManager> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
            _workerAuthenticator = workerAuthenticator;
            _workers = new ConcurrentDictionary<Guid, Worker>();
            _scheduler = new EventLoopScheduler();
            _updates = new Subject<ViewUpdate<WorkerView>>();
        }

        public IObservable<ViewUpdate<WorkerView>> Updates => _updates.AsObservable();

        public Task<Guid> NewWorker(WebSocket socket)
        {
            _logger.LogDebug("New worker connecting");
            var worker = new Worker(socket, _workerAuthenticator);
            TaskPoolScheduler.Default.Schedule(worker.StartReceive);
            _scheduler.Schedule(TimeSpan.FromMilliseconds(500), () => KickIfUnregistered(worker.Id));
            worker.Update.ObserveOn(_scheduler).Subscribe(WorkerUpdate);
            _updates.OnNext(worker.ToView().ToUpdate(UpdateType.Add));
            
            _workers[worker.Id] = worker;
            return worker.Task;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _tickSubscription = Observable.Interval(TimeSpan.FromSeconds(5))
                .ObserveOn(_scheduler)
                .Subscribe(_ => Tick());

            _dispatcher.BinaryReceived.Subscribe(id => _workers[id].RequestStatus());
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _tickSubscription?.Dispose();
            return Task.CompletedTask;
        }

        public WorkerView[] GetAllViews()
        {
            return _workers.Values.Select(w => w.ToView()).ToArray();
        }
        
        /// <summary>
        /// Is invoked on the event loop scheduler when something about a worker has changed.
        /// </summary>
        /// <param name="id">The GUID of the worker who has just had an update</param>
        private void WorkerUpdate(Guid id)
        {
            var retrieved = _workers.TryGetValue(id, out var worker);
            if (!retrieved || worker is null) return;
            
            var view = worker.ToView();
            
            // If the worker is disconnected, in ten seconds we will schedule an attempted removal from the list
            if (view.State == WorkerState.Disconnected)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(10), () => Remove(id)); 
                _dispatcher.NotifyWorkerDisconnect(view.Id);
            }
            
            // If the worker's state is Registered that means it just completed registration and we should prompt it
            // for a status update
            if (view.State == WorkerState.Registered)
                worker.RequestStatus();

            if (view.State == WorkerState.Ready)
            {
                _dispatcher.AddReadyWorker(worker);
            }
            _updates.OnNext(view.ToUpdate(UpdateType.Update));
        }

        private void Remove(Guid id)
        {
            if (_workers.TryGetValue(id, out var worker))
            {
                if (worker.State == WorkerState.Disconnected)
                {
                    _workers.TryRemove(worker.Id, out var _);
                    _updates.OnNext(new ViewUpdate<WorkerView>{Type = UpdateType.Delete, View=worker.ToView()});
                }
            }
        }

        private void KickIfUnregistered(Guid id)
        {
            _logger.LogDebug("Checking {0} for registration", id);
            if (_workers[id].State is WorkerState.WaitingForRegistration or WorkerState.FailedRegistration)
            {
                _logger.LogInformation("Kicking {0} for lack of registration", id);
                _workers.TryRemove(id, out var worker);
                worker?.Dispose();
            }
        }

        private void Tick()
        {
            // Check any workers with unknown state
            foreach (var worker in _workers.Values)
            {
                if (worker.State == WorkerState.Registered)
                {
                    worker.RequestStatus();
                }
            }
        }

        public void Dispose()
        {
            _tickSubscription?.Dispose();
        }
    }
}
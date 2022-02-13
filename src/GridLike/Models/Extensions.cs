using GridLike.Data.Models;
using GridLike.Models.Views;
using GridLike.Workers;

namespace GridLike.Models
{
    public static class Extensions
    {
        public static JobView ToView(this Job job)
        {
            return new JobView
            {
                Id = job.Id,
                Status = job.Status,
                Key = job.Key,
                Priority = job.Priority,
                Submitted = job.Submitted,
                Started = job.Started,
                Ended = job.Ended
            };
        }

        public static ViewUpdate<JobView> ToUpdate(this JobView view, UpdateType type)
        {
            return new ViewUpdate<JobView> { View = view, Type = type };
        }

        public static WorkerView ToView(this Worker worker, string? jobKey = null)
        {
            return new WorkerView
            {
                Id = worker.Id,
                Name = worker.Name,
                ConnectedAt = worker.ConnectedAt,
                DisconnectedAt = worker.DisconnectedAt,
                State = worker.State,
                JobKey = jobKey
            };
        }

        public static ViewUpdate<WorkerView> ToUpdate(this WorkerView view, UpdateType type)
        {
            return new ViewUpdate<WorkerView> { View = view, Type = type };
        }
        public static TimeSpan Age(this Job job) => DateTime.UtcNow - job.Submitted;

    }
}
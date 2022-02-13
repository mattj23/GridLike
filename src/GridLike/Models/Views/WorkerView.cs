using GridLike.Workers;

namespace GridLike.Models.Views
{
    public record WorkerView
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public DateTime ConnectedAt { get; init; }
        public DateTime? DisconnectedAt { get; init; }
        public WorkerState State { get; init; }

        public string? JobKey { get; init; } = default!;
        
    }
}
using System;
using GridLike.Workers;

namespace GridLike.Data.Views
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
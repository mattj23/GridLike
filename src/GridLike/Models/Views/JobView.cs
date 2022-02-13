using GridLike.Data.Models;

namespace GridLike.Models.Views
{
    public record JobView
    {
        public Guid Id { get; init; }
        public JobStatus Status { get; init; }
        public string Key { get; init; } = default!;
        public string? Display { get; init; }
        
        public JobPriority Priority { get; init; }
        public DateTime Submitted { get; init; }
        public DateTime Started { get; init; }
        public DateTime Ended { get; init; }
        public Guid? WorkerId { get; init; }
    }
}
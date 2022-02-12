using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GridLike.Workers
{
    public enum MessageCode
    {
        Register = 0,
        Status = 1,
        StatusRequest = 2,
        Progress = 3,
        JobFailed = 4,
    }

    public enum WorkerStatusCode
    {
        Busy = 0,
        Ready = 1
    }
    
    public record BaseMessage
    {
        public MessageCode Code { get; set; }
    }

    public record RegisterMessage : BaseMessage
    {
        public string Name { get; set; } = null!;
        public string Token { get; set; } = null!;
    }

    public record StatusMessage : BaseMessage
    {
        public WorkerStatusCode Status { get; set; }
    }
    
    public record ProgressMessage : BaseMessage
    {
        public double? Percent { get; set; }
        public string? Info { get; set; }
    }

    public record JobFailedMessage : BaseMessage
    {
        public string? Info { get; set; }
        public string? Logs { get; set; }
    }
}
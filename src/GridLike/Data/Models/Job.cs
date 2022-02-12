using System;
using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models
{
    public enum JobStatus
    {
        Pending,
        Running,
        Done,
        Failed
    }

    public enum JobPriority
    {
        Batch,
        Immediate
    }
    
    public class Job
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(32)] 
        public string Key { get; set; } = null!;

        public string Type { get; set; } = null!;
        
        [MaxLength(128)]
        public string? Display { get; set; }
        
        public JobStatus Status { get; set; }
        public JobPriority Priority { get; set; }
        
        public DateTime Submitted { get; set; }
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
        
        public int FailureCount { get; set; }
        
        public string WorkerId { get; set; }
    }
}
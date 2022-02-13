using System;
using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models
{
    /// <summary>
    /// A Job class is the metadata conceptually attached to a binary input payload. It keeps a record of properties
    /// attached to the job in order for the job registry and dispatcher to track its state and manage its operations.
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Gets or sets the primary GUID by which this job is identified.
        /// </summary>
        [Key] public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets an optional string key which will be used to identify the job to clients in place of the
        /// GUID. Must be unique if specified.
        /// </summary>
        [MaxLength(32)] public string? Key { get; set; }

        /// <summary>
        /// Gets or sets a string indicating the job type. If that feature is not enabled, this will always remain null,
        /// otherwise it needs to be specified to match a valid job type.
        /// </summary>
        [MaxLength(32)] public string? Type { get; set; }
        
        /// <summary>
        /// Gets or sets an integer which includes this job in a batch.  Batches are akin to pages, and allow querying
        /// clients a means to systematically work through all jobs without ever requesting more than a batch size of
        /// information at once.
        /// </summary>
        public int BatchId { get; set; }
        
        /// <summary>
        /// Gets or sets an optional GUID of another job whose result is the input payload for this job. This will
        /// only happen when multiple job types are configured and this job is created from the result of a job of a
        /// different type.
        /// </summary>
        public Guid? FromId { get; set; }
        
        /// <summary>
        /// Gets or sets the job status enum
        /// </summary>
        public JobStatus Status { get; set; }
        
        /// <summary>
        /// Gets or sets the priority enum
        /// </summary>
        public JobPriority Priority { get; set; }
        
        /// <summary>
        /// Gets the UTC timestamp of when the job was submitted to the server
        /// </summary>
        public DateTime Submitted { get; set; }
        
        /// <summary>
        /// Gets the UTC timestamp of the last time the job was sent to a worker for processing
        /// </summary>
        public DateTime Started { get; set; }
        
        /// <summary>
        /// Gets the UTC timestamp of when the job was finished
        /// </summary>
        public DateTime Ended { get; set; }
        
        /// <summary>
        /// Gets the number of times this job was reported as failed by a worker
        /// </summary>
        public int FailureCount { get; set; }
        
        /// <summary>
        /// Gets or sets the integer ID of the last worker to attempt to process this job
        /// </summary>
        public int? WorkerId { get; set; }
    }
    
    public enum JobStatus
    {
        /// <summary>
        /// Starting state of a job when it has been submitted and has not started running. Running can transition
        /// back into this state if the assigned worker disconnects.
        /// </summary>
        Pending,
        
        /// <summary>
        /// The job has been sent to a worker for processing and is presumed to be currently in process.
        /// </summary>
        Running,
        
        /// <summary>
        /// The job is complete and a result payload has been confirmed stored
        /// </summary>
        Done,
        
        /// <summary>
        /// The job was reported to have failed/errored during processing
        /// </summary>
        Failed
    }

    public enum JobPriority
    {
        /// <summary>
        /// Batch priority jobs are run after all non-failed Immediate priority jobs have finished
        /// </summary>
        Batch,
        
        /// <summary>
        /// Immediate jobs are run as soon as a worker is available. 
        /// </summary>
        Immediate
    }
    
}
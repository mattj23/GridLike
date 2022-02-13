using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models;

/// <summary>
/// A JobBatch is a group of jobs which were added at roughly the same time, where the group has a maximum size after
/// which no new jobs will be added to it and the batch is considered stable. A batch serves as a paging mechanism for
/// API clients such that they can deterministically and methodically query through all jobs without having to retrieve
/// all data or dealing with dynamically changing paging as jobs are added and removed.
/// </summary>
public class JobBatch
{
    /// <summary>
    /// Gets or sets the unique integer ID of the batch
    /// </summary>
    [Key] public int Id { get; set; }
    
    /// <summary>
    /// Gets or sets the string type of the job. If job types are not configured on the server this value will always
    /// be null, otherwise it must be a valid job type as specified in the configuration.
    /// </summary>
    public string? JobType { get; set; }
    
    /// <summary>
    /// Gets a flag which indicates that the batch is now stable, and will not have additional jobs added to it. Jobs
    /// may still be removed.
    /// </summary>
    public bool Stable { get; set; }
}
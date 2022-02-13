using System.ComponentModel.DataAnnotations;

namespace GridLike.Data.Models;

/// <summary>
/// The WorkerRecord is an identifying record of a worker indexed by an integer ID, though the UniqueId must also be
/// a unique value.  This is used identify workers when they connect by their UniqueId and retrieve their integer ID,
/// which in turn is used to associated with jobs.
///
/// This is only a useful mechanism if workers are expected to have stable UniqueIds, such as from /etc/machine-id,
/// and so can be disabled in the server configuration. It is used to query completion and throughput statistics for
/// individual workers.
/// </summary>
public class WorkerRecord
{
    /// <summary>
    /// Gets the unique integer ID assigned by the data store for this worker
    /// </summary>
    [Key] public int Id { get; set; }

    /// <summary>
    /// Gets or sets a human readable name for the machine, which does not need to be unique but ideally would be.
    /// Often this would come from a hostname.
    /// </summary>
    [MaxLength(64)] public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets a string which should uniquely and stably identify a worker between disconnections and restarts,
    /// such as the content of /etc/machine-id on modern linux machines. 
    /// </summary>
    [MaxLength(256)] public string UniqueId { get; set; } = null!;
}
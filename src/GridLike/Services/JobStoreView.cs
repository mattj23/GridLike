using System.Collections.Generic;
using GridLike.Data.Views;
using Microsoft.Extensions.Logging;

namespace GridLike.Services
{
    /// <summary>
    /// Maintains a self-updating view of the job store for the razor pages to use to display job information
    /// to the user
    /// </summary>
    public class JobStoreView
    {
        private readonly JobDataStore _jobStore;
        private readonly ILogger<JobStoreView> _logger;

        private readonly List<JobView> _nextJobs;

        public JobStoreView(JobDataStore jobStore, ILogger<JobStoreView> logger)
        {
            _jobStore = jobStore;
            _logger = logger;

            _nextJobs = new List<JobView>();
        }

        public IReadOnlyList<JobView> NextJobs => _nextJobs;
    }
}
using System.Diagnostics;

namespace GridLike.Models;

public static class ServerConfigExtensions
{
    public static ServerConfiguration GetServerConfiguration(this IConfigurationSection? section)
    {
        var defaultConfig = new ServerConfiguration(100, true, Array.Empty<JobTypeConfig>());
        if (section is null) return defaultConfig;

        var batchSize = section.GetValue<int?>("BatchSize") ?? defaultConfig.BatchSize;
        var trackWorkers = section.GetValue<bool?>("TrackWorkers") ?? defaultConfig.TrackWorkers;

        var typeSection = section.GetSection("JobTypes");
        var types = new List<JobTypeConfig>();
        if (typeSection is not null)
        {
            foreach (var child in typeSection.GetChildren())
            {
                var data = child.Get<JobTypeConfigLoad>();
                types.Add(new JobTypeConfig(child.Key, data?.Description, data?.ResultBecomes));
            }
        }

        return new ServerConfiguration(batchSize, trackWorkers, types.ToArray());
    }
    
    private class JobTypeConfigLoad {
        public string? Description { get; set; }
        public string? ResultBecomes { get; set; }
    }
}

public record ServerConfiguration(int BatchSize, bool TrackWorkers, IReadOnlyCollection<JobTypeConfig> JobTypes);
public record JobTypeConfig(string Name, string? Description, string? ResultBecomes);


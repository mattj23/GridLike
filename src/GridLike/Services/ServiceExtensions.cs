using System;
using GridLike.Data;
using GridLike.Data.Models;
using GridLike.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridLike.Services
{
    public static class ServiceExtensions
    {
        public static void UseDatabase(this IServiceCollection services, IConfigurationSection config)
        {
            var typeName = config.GetValue<string>("Type").ToLower();
            var connectionString = config.GetValue<string>("ConnectionString");
            switch (typeName)
            {
                case "sqlite":
                    services.AddDbContext<GridLikeContext>(o => o.UseSqlite(connectionString,
                        x => x.MigrationsAssembly("GridLike.Migrations.Sqlite")));
                    break;
                case "postgres":
                    services.AddDbContext<GridLikeContext>(o => o.UseNpgsql(connectionString,
                        x => x.MigrationsAssembly("GridLike.Migrations.PostgreSQL")));
                    break;
                case "mysql":
                    services.AddDbContext<GridLikeContext>(o => o.UseMySql(connectionString,
                        ServerVersion.AutoDetect(connectionString), 
                        x => x.MigrationsAssembly("GridLike.Migrations.MySQL")));
                    break;
                default:
                    throw new NotSupportedException($"No database provider found for type={typeName}");
            }
        }

        /// <summary>
        /// Applies database migrations to the application's data provider, which will create the database if it does
        /// not exist or update it if it's on an old version.
        /// </summary>
        /// <param name="app"></param>
        /// <exception cref="ApplicationException">thrown if no database context could be retrieved</exception>
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var services = app.ApplicationServices.CreateScope();
            var context = services.ServiceProvider.GetService<GridLikeContext>();
            if (context is null)
            {
                throw new ApplicationException("Attempted database migration with no context available");
            }
            context.Database.Migrate();
        }

        /// <summary>
        /// Validates the application data in the database matches with the static configuration, or applies the
        /// expected data on startup if the database is empty.
        /// </summary>
        /// <param name="app"></param>
        /// <exception cref="ApplicationException">Thrown if there is a configuration error</exception>
        public static void ApplyBaseData(this IApplicationBuilder app)
        {
            // Get the database context
            using var services = app.ApplicationServices.CreateScope();
            var context = services.ServiceProvider.GetService<GridLikeContext>();
            if (context is null)
            {
                throw new ApplicationException("Attempted database migration with no context available");
            }
            
            // Get the server configuration options
            var config = services.ServiceProvider.GetService<ServerConfiguration>();

            // Validate that the job types match what's in the database. If the database is empty, we may create the
            // job types.  Otherwise they need to match what exists or we throw an error.
            var jobTypes = context.JobTypes.ToArray();
            var hasJobs = context.Jobs.Any();

            if (!jobTypes.Any() && !hasJobs)
            {
                // Database is empty, create records as necessary
                foreach (var type in config.JobTypes)
                    context.JobTypes.Add(new JobType { Name = type.Name, Description = type.Description });

                context.SaveChanges();

                var types = context.JobTypes.ToDictionary(t => t.Name, t => t);
                foreach (var type in config.JobTypes)
                {
                    if (type.ResultBecomes is not null)
                    {
                        if (!types.ContainsKey(type.ResultBecomes))
                            throw new ApplicationException(
                                $"The job type {type.Name} specifies a transition into type {type.ResultBecomes}," +
                                $" but that type does not exist in the configuration.");
                            
                        types[type.Name].BecomesId = types[type.ResultBecomes].Id;
                    }
                }

                context.SaveChanges();
            }
            else
            {
                if (jobTypes.Length != config.JobTypes.Count)
                    throw new ApplicationException(
                        $"The database contains {jobTypes.Length} job types but the current configuration contains " +
                        $"{config.JobTypes.Count}. Has the configuration changed since this database was last empty?");
                
                var types = context.JobTypes.ToDictionary(t => t.Name, t => t);
                foreach (var type in config.JobTypes)
                {
                    if (!types.ContainsKey(type.Name))
                        throw new ApplicationException(
                            $"The current configuration has a job type {type.Name}, but the database does not. " +
                            $"Has the configuration changed since the database was last empty?");
                    
                    if (type.ResultBecomes is not null && types[type.ResultBecomes].Id != types[type.Name].BecomesId)
                        throw new ApplicationException(
                            $"The current configuration job type {type.Name} transitions into {type.ResultBecomes}, " +
                            $"but the database does not reflect this. Has the configuration changed since the " +
                            $"database was last empty?");
                        
                    if (type.ResultBecomes is null && types[type.Name].BecomesId is not null)
                        throw new ApplicationException(
                            $"The current configuration job type {type.Name} has no transition but " +
                            $"but the database does not reflect this. Has the configuration changed since the " +
                            $"database was last empty?");
                }
            }
        }
    }
}
using System;
using GridLike.Data;
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


    }
}
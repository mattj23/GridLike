using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridLike.Data;

public class MigrationContextFactory : IDesignTimeDbContextFactory<GridLikeContext>
{
    public GridLikeContext CreateDbContext(string[] args)
    {
        if (args.Length < 1) throw new ArgumentException();

        var builder = new DbContextOptionsBuilder<GridLikeContext>();
        switch (args[0].ToLower().Trim())
        {
            case "sqlite":
                builder.UseSqlite(x => x.MigrationsAssembly("GridLike.Migrations.Sqlite"));
                break;
            case "mysql":
                builder.UseMySql(new MySqlServerVersion(new Version(8, 0, 0)), 
                    x => x.MigrationsAssembly("GridLike.Migrations.MySQL"));
                break;
            case "postgresql":
                builder.UseNpgsql(x => x.MigrationsAssembly("GridLike.Migrations.PostgreSQL"));
                break;
        }

        return new GridLikeContext(builder.Options);
    }
}
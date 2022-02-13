using GridLike.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridLike.Migrations.MySQL;

public class MigrationContextFactory : IDesignTimeDbContextFactory<GridLikeContext>
{
    public GridLikeContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GridLikeContext>();
        builder.UseMySql(new MySqlServerVersion(new Version(8, 0, 0)), 
            x => x.MigrationsAssembly("GridLike.Migrations.MySQL"));
        return new GridLikeContext(builder.Options);
    }
}
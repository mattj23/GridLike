using GridLike.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridLike.Migrations.PostgreSQL;

public class MigrationContextFactory : IDesignTimeDbContextFactory<GridLikeContext>
{
    public GridLikeContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GridLikeContext>();
        builder.UseNpgsql(b => b.MigrationsAssembly("GridLike.Migrations.PostgreSQL"));
        return new GridLikeContext(builder.Options);
    }
}
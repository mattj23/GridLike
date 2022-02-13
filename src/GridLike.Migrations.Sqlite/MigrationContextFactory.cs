using GridLike.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridLike.Migrations.Sqlite;

public class MigrationContextFactory : IDesignTimeDbContextFactory<GridLikeContext>
{
    public GridLikeContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GridLikeContext>();
        builder.UseSqlite(x => x.MigrationsAssembly("GridLike.Migrations.Sqlite"));
        return new GridLikeContext(builder.Options);
    }
}
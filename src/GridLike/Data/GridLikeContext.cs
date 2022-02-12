using GridLike.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GridLike.Data
{
    public class GridLikeContext : DbContext
    {
        public GridLikeContext(DbContextOptions<GridLikeContext> context) : base(context) { }
        
        public DbSet<Job> Jobs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
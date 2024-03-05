using Microsoft.EntityFrameworkCore;

namespace RegistryV2
{
    public class AppDbContext : DbContext
    {
        public DbSet<Petition> Petition { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {

        }
    }
}
